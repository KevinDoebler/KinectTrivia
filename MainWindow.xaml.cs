
//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

// This module contains code to do Kinect NUI initialization,
// processing, displaying players on screen, and sending updated player
// positions to the game portion for hit testing.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.BackgroundRemoval;
using Microsoft.Samples.Kinect.WpfViewers;
using TriviaGame.Properties;
using TriviaGame.Speech;
using TriviaGame.Utils;

namespace TriviaGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow
    {
        public static readonly DependencyProperty KinectSensorManagerProperty =
            DependencyProperty.Register(
                "KinectSensorManager",
                typeof(KinectSensorManager),
                typeof(MainWindow),
                new PropertyMetadata(null));

        #region Members

        private const int TimerResolution = 2;  // ms
        private const int NumIntraFrames = 3;
        private const int MaxShapes = 4;
        private int playersAlive;
        private int frameCount;



        private double MaxFramerate = Settings.Default.MaxFrameRate;
        private double MinFramerate = Settings.Default.MinFrameRate;
        private double targetFramerate = Settings.Default.MaxFrameRate;

        private const double MinShapeSize = 100;
        private const double MaxShapeSize = 100;
        private const double DefaultDropRate = 2.0;
        private const double DefaultDropSize = 60.0;
        private const double DefaultDropGravity = 0.5;
        
        private double dropRate = DefaultDropRate;
        private double dropSize = DefaultDropSize;
        private double dropGravity = DefaultDropGravity;
        private double actualFrameTime;

        private readonly Dictionary<int, Player> players = new Dictionary<int, Player>();

        // Player(s) placement in scene (z collapsed):
        private Rect playerBounds;
        private Rect screenRect;
        
        private bool runningGameThread;

        private DateTime lastFrameDrawn = DateTime.MinValue;
        private DateTime predNextFrame = DateTime.MinValue;

        /// <summary>
        /// the skeleton that is currently tracked by the app
        /// </summary>
        private int currentlyTrackedSkeletonId;

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed;

        private readonly SoundPlayer soundPop = new SoundPlayer();
        private readonly SoundPlayer soundHit = new SoundPlayer();
        private readonly SoundPlayer soundSqueeze = new SoundPlayer();
        private readonly SoundPlayer soundTestFootballKnowledge = new SoundPlayer();
        private readonly SoundPlayer soundFoxTheme = new SoundPlayer();
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();

        private Skeleton[] skeletonData;

        private FallingThings myFallingThings;

        private SpeechRecognizer mySpeechRecognizer;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap foregroundBitmap;

        /// <summary>
        /// Intermediate storage for the skeleton data received from the sensor
        /// </summary>
        private Skeleton[] skeletons;

        /// <summary>
        /// Format we will use for the depth stream
        /// </summary>
        private const DepthImageFormat DepthFormat = DepthImageFormat.Resolution320x240Fps30;

        /// <summary>
        /// Format we will use for the color stream
        /// </summary>
        private const ColorImageFormat ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Our core library which does background 
        /// </summary>
        private BackgroundRemovedColorStream backgroundRemovedColorStream;

        #endregion Private State

        #region ctor + Window Events

        public MainWindow()
        {
            KinectSensorManager = new KinectSensorManager();
            DataContext = KinectSensorManager;

            InitializeComponent();

            // initialize the sensor chooser and UI
            sensorChooserUI.KinectSensorChooser = sensorChooser;
            sensorChooser.KinectChanged += SensorChooserOnKinectChanged;

            sensorChooser.Start();

            // Responsible for video preview
            // Bind the KinectSensor from the sensorChooser to the KinectSensor on the KinectSensorManager
            var kinectSensorBinding = new Binding("Kinect") { Source = sensorChooser };
            BindingOperations.SetBinding(KinectSensorManager, KinectSensorManager.KinectSensorProperty, kinectSensorBinding);
        }

        /// <summary>
        /// Finalizes an instance of the MainWindow class.
        /// This destructor will run only if the Dispose method does not get called.
        /// </summary>
        ~MainWindow()
        {
            Dispose(false);
        }

        private void btnSetAngle_Click(object sender, RoutedEventArgs e)
        {
            KinectSensorManager.KinectSensor.ElevationAngle = (int)sliderAngle.Value;
        }

        private void sliderAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            btnSetAngle.Content = (int)sliderAngle.Value;
        }

        /// <summary>
        /// Handles the checking or unchecking of the near mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxNearModeChanged(object sender, RoutedEventArgs e)
        {
            if (null == sensorChooser || null == sensorChooser.Kinect)
            {
                return;
            }

            // will not function on non-Kinect for Windows devices
            try
            {
                sensorChooser.Kinect.DepthStream.Range = checkBoxNearMode.IsChecked.GetValueOrDefault()
                    ? DepthRange.Near
                    : DepthRange.Default;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void EnableAecChecked(object sender, RoutedEventArgs e)
        {
            var enableAecCheckBox = (CheckBox)sender;
            UpdateEchoCancellation(enableAecCheckBox);
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (sensorChooser.Kinect != null)
            {
                sensorChooser.Kinect.Stop();
            }

            if (sensorChooser != null)
            {
                sensorChooser.Stop();
            }

            runningGameThread = false;
            Settings.Default.PrevWinPosition = RestoreBounds;
            Settings.Default.WindowState = (int)WindowState;
            Settings.Default.Save();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            KinectSensorManager.KinectSensor = null;
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            playfield.ClipToBounds = true;

            //Have player enter name (Using speech recognition. Letter by letter or whole word. Whatever works)

            //Have player select difficulty. (Again, with speech recognition)

            myFallingThings = new FallingThings(MaxShapes, targetFramerate, NumIntraFrames, ref lblQuestion);

            UpdatePlayfieldSize();

            myFallingThings.AskNewQuestion();
            myFallingThings.SetGravity(dropGravity);
            myFallingThings.SetDropRate(dropRate);
            myFallingThings.SetSize(dropSize);
            myFallingThings.SetPolies(DropType.All);
            myFallingThings.SetGameMode(GameMode.Off);

            soundPop.Stream = Properties.Resources.Pop_5;
            soundHit.Stream = Properties.Resources.Hit_2;
            soundSqueeze.Stream = Properties.Resources.Squeeze;
            soundTestFootballKnowledge.Stream = Properties.Resources.testyourfootballknowledge;
            soundFoxTheme.Stream = Properties.Resources.fox;

            soundPop.Play();

            TimeBeginPeriod(TimerResolution);

            // xxx

            var myGameThread = new Thread(GameThread);
            myGameThread.SetApartmentState(ApartmentState.STA);
            myGameThread.Start();

            FlyingText.NewFlyingText(screenRect.Width / 30, new Point(screenRect.Width / 2, screenRect.Height / 2), "NFL Trivia!!");

            //soundTestFootballKnowledge.Play();

            //soundFoxTheme.Play();
        }

        private void RestoreWindowState()
        {
            // Restore window state to that last used
            Rect bounds = Settings.Default.PrevWinPosition;
            
            if (bounds.Right != bounds.Left)
            {
                Top = bounds.Top;
                Left = bounds.Left;
                Height = bounds.Height;
                Width = bounds.Width;
            }

            WindowState = (WindowState)Settings.Default.WindowState;
        }

        /// <summary>
        /// Dispose the allocated frame buffers and reconstruction.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees all memory associated with the FusionImageFrame.
        /// </summary>
        /// <param name="disposing">Whether the function was called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (null != backgroundRemovedColorStream)
                {
                    backgroundRemovedColorStream.Dispose();
                    backgroundRemovedColorStream = null;
                }

                disposed = true;
            }
        }

        #endregion ctor + Window Events

        #region Kinect discovery + setup

        public KinectSensorManager KinectSensorManager
        {
            get { return (KinectSensorManager)GetValue(KinectSensorManagerProperty); }
            set { SetValue(KinectSensorManagerProperty, value); }
        }

        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event arguments</param>
        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.AllFramesReady -= SensorAllFramesReady;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.ColorStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();

                    // Create the background removal stream to process the data and remove background, and initialize it.
                    if (null != backgroundRemovedColorStream)
                    {
                        backgroundRemovedColorStream.BackgroundRemovedFrameReady -= BackgroundRemovedFrameReadyHandler;
                        backgroundRemovedColorStream.Dispose();
                        backgroundRemovedColorStream = null;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                  

                    args.NewSensor.DepthStream.Enable(DepthFormat);
                    args.NewSensor.ColorStream.Enable(ColorFormat);
                    args.NewSensor.SkeletonStream.Enable();

                    backgroundRemovedColorStream = new BackgroundRemovedColorStream(args.NewSensor);
                    backgroundRemovedColorStream.Enable(ColorFormat, DepthFormat);

                    // Allocate space to put the depth, color, and skeleton data we'll receive
                    if (null == skeletons)
                    {
                        skeletons = new Skeleton[args.NewSensor.SkeletonStream.FrameSkeletonArrayLength];
                    }

                    // Add an event handler to be called when the background removed color frame is ready, so that we can
                    // composite the image and output to the app
                    backgroundRemovedColorStream.BackgroundRemovedFrameReady += BackgroundRemovedFrameReadyHandler;

                    // Add an event handler to be called whenever there is new depth frame data
                    args.NewSensor.AllFramesReady += SensorAllFramesReady;

                    try
                    {
                        args.NewSensor.DepthStream.Range = checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                   : DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;

                        // Skeleton
                        args.NewSensor.SkeletonFrameReady += SkeletonsReady;
                        KinectSensorManager.TransformSmoothParameters = new TransformSmoothParameters
                        {
                            Smoothing = 0.5f,
                            Correction = 0.5f,
                            Prediction = 0.5f,
                            JitterRadius = 0.05f,
                            MaxDeviationRadius = 0.04f
                        };
                        KinectSensorManager.SkeletonStreamEnabled = true;
                        KinectSensorManager.KinectSensorEnabled = true;

                        sliderAngle.Value = Settings.Default.DefaultElevationAngle;
                        args.NewSensor.ElevationAngle = Settings.Default.DefaultElevationAngle;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    // this.statusBarText.Text = Properties.Resources.ReadyForScreenshot;
                }
                catch (InvalidOperationException ex)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // in the middle of shutting down, or lingering events from previous sensor, do nothing here.
            if (null == sensorChooser || null == sensorChooser.Kinect || sensorChooser.Kinect != sender)
            {
                return;
            }

            try
            {
                using (var depthFrame = e.OpenDepthImageFrame())
                {
                    if (null != depthFrame)
                    {
                        backgroundRemovedColorStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                    }
                }

                using (var colorFrame = e.OpenColorImageFrame())
                {
                    if (null != colorFrame)
                    {
                        backgroundRemovedColorStream.ProcessColor(colorFrame.GetRawPixelData(), colorFrame.Timestamp);
                    }
                }

                using (var skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (null != skeletonFrame)
                    {
                        skeletonFrame.CopySkeletonDataTo(skeletons);
                        backgroundRemovedColorStream.ProcessSkeleton(skeletons, skeletonFrame.Timestamp);
                    }
                }

                ChooseSkeleton();
            }
            catch (InvalidOperationException)
            {
                // Ignore the exception. 
            }
        }

        /// <summary>
        /// Handle the background removed color frame ready event. The frame obtained from the background removed
        /// color stream is in RGBA format.
        /// </summary>
        /// <param name="sender">object that sends the event</param>
        /// <param name="e">argument of the event</param>
        private void BackgroundRemovedFrameReadyHandler(object sender, BackgroundRemovedColorFrameReadyEventArgs e)
        {
            using (var backgroundRemovedFrame = e.OpenBackgroundRemovedColorFrame())
            {
                if (backgroundRemovedFrame != null)
                {
                    if (null == foregroundBitmap || foregroundBitmap.PixelWidth != backgroundRemovedFrame.Width
                        || foregroundBitmap.PixelHeight != backgroundRemovedFrame.Height)
                    {
                        foregroundBitmap = new WriteableBitmap(backgroundRemovedFrame.Width, backgroundRemovedFrame.Height, 96.0, 96.0, PixelFormats.Bgra32, null);

                        // Set the image we display to point to the bitmap where we'll put the image data
                        MaskedColor.Source = foregroundBitmap;
                    }

                    // Write the pixel data into our bitmap
                    foregroundBitmap.WritePixels(
                        new Int32Rect(0, 0, foregroundBitmap.PixelWidth, foregroundBitmap.PixelHeight),
                        backgroundRemovedFrame.GetRawPixelData(),
                        foregroundBitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }

        // Kinect enabled apps should uninitialize all Kinect services that were initialized in InitializeKinectServices() here.
        private void UninitializeKinectServices(KinectSensor sensor)
        {
            sensor.SkeletonFrameReady -= SkeletonsReady;

            if (null != mySpeechRecognizer)
            {
                mySpeechRecognizer.Stop();
                mySpeechRecognizer.SaidSomething -= RecognizerSaidSomething;
                mySpeechRecognizer.Dispose();
                mySpeechRecognizer = null;
            }

            enableAec.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Use the sticky skeleton logic to choose a player that we want to set as foreground. This means if the app
        /// is tracking a player already, we keep tracking the player until it leaves the sight of the camera, 
        /// and then pick the closest player to be tracked as foreground.
        /// </summary>
        private void ChooseSkeleton()
        {
            var isTrackedSkeltonVisible = false;
            var nearestDistance = float.MaxValue;
            var nearestSkeleton = 0;

            foreach (var skel in skeletons)
            {
                if (null == skel)
                {
                    continue;
                }

                if (skel.TrackingState != SkeletonTrackingState.Tracked)
                {
                    continue;
                }

                if (skel.TrackingId == currentlyTrackedSkeletonId)
                {
                    isTrackedSkeltonVisible = true;
                    break;
                }

                if (skel.Position.Z < nearestDistance)
                {
                    nearestDistance = skel.Position.Z;
                    nearestSkeleton = skel.TrackingId;
                }
            }

            if (!isTrackedSkeltonVisible && nearestSkeleton != 0)
            {
                backgroundRemovedColorStream.SetTrackedPlayer(nearestSkeleton);
                currentlyTrackedSkeletonId = nearestSkeleton;
            }
        }

        #endregion Kinect discovery + setup

        #region Kinect Skeleton processing
        private void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    int skeletonSlot = 0;

                    if ((skeletonData == null) || (skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(skeletonData);

                    foreach (Skeleton skeleton in skeletonData)
                    {
                        if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                        {
                            Player player;
                            if (players.ContainsKey(skeletonSlot))
                            {
                                player = players[skeletonSlot];
                            }
                            else
                            {
                                player = new Player(skeletonSlot);
                                player.SetBounds(playerBounds);
                                players.Add(skeletonSlot, player);
                            }

                            player.LastUpdated = DateTime.Now;

                            // Update player's bone and joint positions
                            if (skeleton.Joints.Count > 0)
                            {
                                player.IsAlive = true;

                                // Head, hands, feet (hit testing happens in order here)
                                player.UpdateJointPosition(skeleton.Joints, JointType.Head);
                                player.UpdateJointPosition(skeleton.Joints, JointType.HandLeft);
                                player.UpdateJointPosition(skeleton.Joints, JointType.HandRight);
                                player.UpdateJointPosition(skeleton.Joints, JointType.FootLeft);
                                player.UpdateJointPosition(skeleton.Joints, JointType.FootRight);

                                // Hands and arms
                                player.UpdateBonePosition(skeleton.Joints, JointType.HandRight, JointType.WristRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.WristRight, JointType.ElbowRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ElbowRight, JointType.ShoulderRight);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HandLeft, JointType.WristLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.WristLeft, JointType.ElbowLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ElbowLeft, JointType.ShoulderLeft);

                                // Head and Shoulders
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter, JointType.Head);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderLeft, JointType.ShoulderCenter);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderRight);

                                // Legs
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.KneeLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HipRight, JointType.KneeRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.KneeRight, JointType.AnkleRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.AnkleRight, JointType.FootRight);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.HipCenter);
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.HipRight);

                                // Spine
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.ShoulderCenter);
                            }
                        }

                        skeletonSlot++;
                    }
                }
            }
        }

        private void CheckPlayers()
        {
            foreach (var player in players)
            {
                if (!player.Value.IsAlive)
                {
                    // Player left scene since we aren't tracking it anymore, so remove from dictionary
                    players.Remove(player.Value.GetId());
                    break;
                }
            }

            // Count alive players
            int alive = players.Count(player => player.Value.IsAlive);

            if (alive != playersAlive)
            {
                if (alive == 2)
                {
                    myFallingThings.SetGameMode(GameMode.TwoPlayer);
                }
                else if (alive == 1)
                {
                    myFallingThings.SetGameMode(GameMode.Solo);
                }
                else if (alive == 0)
                {
                    myFallingThings.SetGameMode(GameMode.Off);
                }

                if ((playersAlive == 0) && (mySpeechRecognizer != null))
                {
                    BannerText.NewBanner(
                        Properties.Resources.Vocabulary,
                        screenRect,
                        true,
                        Color.FromArgb(200, 255, 255, 255));
                }

                playersAlive = alive;
            }
        }

        private void PlayfieldSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePlayfieldSize();
        }

        private void UpdatePlayfieldSize()
        {
            // Size of player wrt size of playfield, putting ourselves low on the screen.
            screenRect.X = 0;
            screenRect.Y = 0;
            screenRect.Width = playfield.ActualWidth;
            screenRect.Height = playfield.ActualHeight;

            BannerText.UpdateBounds(screenRect);

            playerBounds.X = 0;
            playerBounds.Width = playfield.ActualWidth;
            playerBounds.Y = playfield.ActualHeight * 0.2;
            playerBounds.Height = playfield.ActualHeight * 0.75;

            foreach (var player in players)
            {
                player.Value.SetBounds(playerBounds);
            }

            Rect fallingBounds = playerBounds;
            fallingBounds.Y = 0;
            fallingBounds.Height = playfield.ActualHeight;
            if (myFallingThings != null)
            {
                myFallingThings.SetBoundaries(fallingBounds);
            }
        }

        #endregion Kinect Skeleton processing

        #region Kinect Speech processing
        private void RecognizerSaidSomething(object sender, SpeechRecognizer.SaidSomethingEventArgs e)
        {
            return;
            if (e.Matched != "?")
            {
                FlyingText.NewFlyingText(screenRect.Width / 30, new Point(screenRect.Width / 2, screenRect.Height / 2),
                                         e.Matched);
                switch (e.Verb)
                {
                    case SpeechRecognizer.Verbs.Easy:
                        myFallingThings.SetGameDifficulty(1);
                        break;

                    case SpeechRecognizer.Verbs.Medium:
                        myFallingThings.SetGameDifficulty(2);
                        break;

                    case SpeechRecognizer.Verbs.Hard:
                        myFallingThings.SetGameDifficulty(3);
                        break;

                    case SpeechRecognizer.Verbs.Pause:
                        myFallingThings.SetDropRate(0);
                        myFallingThings.SetGravity(0);
                        break;
                    case SpeechRecognizer.Verbs.Resume:
                        myFallingThings.SetDropRate(dropRate);
                        myFallingThings.SetGravity(dropGravity);
                        break;
                    case SpeechRecognizer.Verbs.Reset:
                        dropRate = DefaultDropRate;
                        dropSize = DefaultDropSize;
                        dropGravity = DefaultDropGravity;
                        myFallingThings.SetPolies(DropType.All);
                        myFallingThings.SetDropRate(dropRate);
                        myFallingThings.SetGravity(dropGravity);
                        myFallingThings.SetSize(dropSize);
                        myFallingThings.SetShapesColor(Color.FromRgb(0, 0, 0), true);
                        myFallingThings.Reset();
                        break;
                    case SpeechRecognizer.Verbs.DoShapes:
                        myFallingThings.SetPolies(e.Shape);
                        break;
                    case SpeechRecognizer.Verbs.RandomColors:
                        myFallingThings.SetShapesColor(Color.FromRgb(0, 0, 0), true);
                        break;
                    case SpeechRecognizer.Verbs.Colorize:
                        myFallingThings.SetShapesColor(e.RgbColor, false);
                        break;
                    case SpeechRecognizer.Verbs.ShapesAndColors:
                        myFallingThings.SetPolies(e.Shape);
                        myFallingThings.SetShapesColor(e.RgbColor, false);
                        break;
                    case SpeechRecognizer.Verbs.More:
                        dropRate *= 1.5;
                        myFallingThings.SetDropRate(dropRate);
                        break;
                    case SpeechRecognizer.Verbs.Fewer:
                        dropRate /= 1.5;
                        myFallingThings.SetDropRate(dropRate);
                        break;
                    case SpeechRecognizer.Verbs.Bigger:
                        dropSize *= 1.5;
                        if (dropSize > MaxShapeSize)
                        {
                            dropSize = MaxShapeSize;
                        }

                        myFallingThings.SetSize(dropSize);
                        break;
                    case SpeechRecognizer.Verbs.Biggest:
                        dropSize = MaxShapeSize;
                        myFallingThings.SetSize(dropSize);
                        break;
                    case SpeechRecognizer.Verbs.Smaller:
                        dropSize /= 1.5;
                        if (dropSize < MinShapeSize)
                        {
                            dropSize = MinShapeSize;
                        }

                        myFallingThings.SetSize(dropSize);
                        break;
                    case SpeechRecognizer.Verbs.Smallest:
                        dropSize = MinShapeSize;
                        myFallingThings.SetSize(dropSize);
                        break;
                    case SpeechRecognizer.Verbs.Faster:
                        dropGravity *= 1.25;
                        if (dropGravity > 4.0)
                        {
                            dropGravity = 4.0;
                        }

                        myFallingThings.SetGravity(dropGravity);
                        break;
                    case SpeechRecognizer.Verbs.Slower:
                        dropGravity /= 1.25;
                        if (dropGravity < 0.25)
                        {
                            dropGravity = 0.25;
                        }

                        myFallingThings.SetGravity(dropGravity);
                        break;
                }
            }
        }

        private void UpdateEchoCancellation(CheckBox aecCheckBox)
        {
            mySpeechRecognizer.EchoCancellationMode = aecCheckBox.IsChecked != null && aecCheckBox.IsChecked.Value
                ? EchoCancellationMode.CancellationAndSuppression
                : EchoCancellationMode.None;
        }

        #endregion Kinect Speech processing

        #region GameTimer/Thread
        private void GameThread()
        {
            runningGameThread = true;
            predNextFrame = DateTime.Now;
            actualFrameTime = 1000.0 / targetFramerate;

            // Try to dispatch at as constant of a framerate as possible by sleeping just enough since
            // the last time we dispatched.
            while (runningGameThread)
            {
                // Calculate average framerate.  
                DateTime now = DateTime.Now;
                if (lastFrameDrawn == DateTime.MinValue)
                {
                    lastFrameDrawn = now;
                }

                double ms = now.Subtract(lastFrameDrawn).TotalMilliseconds;
                actualFrameTime = (actualFrameTime * 0.95) + (0.05 * ms);
                lastFrameDrawn = now;

                // Adjust target framerate down if we're not achieving that rate
                frameCount++;
                if ((frameCount % 100 == 0) && (1000.0 / actualFrameTime < targetFramerate * 0.92))
                {
                    targetFramerate = Math.Max(MinFramerate, (targetFramerate + (1000.0 / actualFrameTime)) / 2);
                }

                if (now > predNextFrame)
                {
                    predNextFrame = now;
                }
                else
                {
                    double milliseconds = predNextFrame.Subtract(now).TotalMilliseconds;
                    if (milliseconds >= TimerResolution)
                    {
                        Thread.Sleep((int)(milliseconds + 0.5));
                    }
                }

                predNextFrame += TimeSpan.FromMilliseconds(1000.0 / targetFramerate);

                Dispatcher.Invoke(DispatcherPriority.Send, new Action<int>(HandleGameTimer), 0);
            }
        }

        private void HandleGameTimer(int param)
        {
            // Every so often, notify what our actual framerate is
            if ((frameCount % 100) == 0)
            {
                myFallingThings.SetFramerate(1000.0 / actualFrameTime);
            }

            // Advance animations, and do hit testing.
            for (int i = 0; i < NumIntraFrames; ++i)
            {
                foreach (var pair in players)
                {
                    HitType hit = myFallingThings.LookForHits(pair.Value.Segments, pair.Value.GetId());
                    if ((hit & HitType.Correct) != 0)
                    {
                        soundSqueeze.Play();    
                        FlyingText.NewFlyingText(screenRect.Width / 30, new Point(screenRect.Width / 2, screenRect.Height / 2), "YES!");
                    }
                    else if ((hit & HitType.Incorrect) != 0)
                    {
                        soundPop.Play();
                        FlyingText.NewFlyingText(screenRect.Width / 30, new Point(screenRect.Width / 2, screenRect.Height / 2), "Wrong :(");
                    }
                }

                myFallingThings.AdvanceFrame();
            }

            // Draw new Wpf scene by adding all objects to canvas
            // xxx
            playfield.Children.Clear();
            myFallingThings.DrawFrame(playfield.Children);
            foreach (var player in players)
            {
                player.Value.Draw(playfield.Children);
            }

            //xxx
            BannerText.Draw(playfield.Children);
            FlyingText.Draw(playfield.Children);

            CheckPlayers();
        }

        // Since the timer resolution defaults to about 10ms precisely, we need to
        // increase the resolution to get framerates above between 50fps with any
        // consistency.
        [DllImport("Winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern int TimeBeginPeriod(uint period);

        #endregion GameTimer/Thread
    }
}
