   M S B A M L   `   `   `  ����^  ZMicrosoft.Samples.Kinect.WpfViewers, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null�^clr-namespace:Microsoft.Samples.Kinect.WpfViewers;assembly=Microsoft.Samples.Kinect.WpfViewers#Microsoft.Samples.Kinect.WpfViewers  5   	   S OMicrosoft.Kinect.Toolkit, Version=1.5.2.0, Culture=neutral, PublicKeyToken=nulleHclr-namespace:Microsoft.Kinect.Toolkit;assembly=Microsoft.Kinect.ToolkitMicrosoft.Kinect.Toolkit 5   	    
TriviaGame   TriviaGame.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   kkt^clr-namespace:Microsoft.Samples.Kinect.WpfViewers;assembly=Microsoft.Samples.Kinect.WpfViewers    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   \toolkitHclr-namespace:Microsoft.Kinect.Toolkit;assembly=Microsoft.Kinect.Toolkit     5   	   -   5          � Title$  Kinect Shape Game��5   	   $��Italic$�6#    � SizeToContent$ Manual=�66    � WindowStartupLocation$ CenterScreen=�5   M    � WindowState$ Normal=�6r   $
��1440��6�   $	��810��6�   ���	#F73CA8FF.��5      � -   5        $��
layoutGrid��5      $��	10 0 10 0q�6   $��Stretch=�60   $��Stretch=�6N    � RowDefinitions 5	   
   �� 5
      $	��100 �6   �� 5      $	��100 �6   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   � $	��Top=�6   $��0��6(   ���	#FF13C2C9 � ColumnDefinitions 5      �� 5      $	��55* �6#   �� 5      $
��410* �6#   �� 5      $
��947* �6#   .+�5      4  .Microsoft.Kinect.Toolkit.KinectSensorChooserUI  -   5        $��sensorChooserUI��5   �   $��2��6,   $��Center=�6<   $��221,0,686,0q�6Y   $	��Top=�6n   6�   ;   5Microsoft.Samples.Kinect.WpfViewers.KinectColorViewer  5      $��2��6#   $��
820,0,17,0q�63   $	��100��6G   $	��Top=�6T    K� ZIndex$	 100��6�   7   1Microsoft.Samples.Kinect.WpfViewers.KinectControl   KinectSensorManager 6l   �� 6�   �� 5      -   5        $��sliderAngle��5   M   $��2��6   $��15��6%   $
��Left=�61   $	��Top=�6`   $	��384��6x    � Maximum$ 27Q�6�   	 � Minimum$		 -27Q�6�   
 �� Orientation$
 
Horizontal=�6�   $��50,5,0,0q�6�   �� 5      -   5        $��btnSetAngle��5   a   $��Adjust��6   $��25��6'   $
��Left=�63   $��	450,3,0,0q�6N   $	��Top=�6t   $	��135��6�    J� RenderTransformOrigin$ 1,-1.5&�6�   $��2��6�   5      � 5   
   $��1��6   .+�5      �� -   5        $��lblQuestion��5      $��24(�6'   
����������	#FF3CA8FF$��Futura Std Medium)�6_   $��	0,0,0,-95q�6~   $
��1400��6�   $	��164��6�   5       � 5!   
   $��2��6   .+�5"      �� -   5        $��	playfield��5"   +   $��10,10,10,9.5q�6   ��5#      �� 5$       �� ImageSource$! Resources/stadiumResize.jpg��6!   5&      5'      �� 5(   
   -   5        $��MaskedColor��5(      $ 50000��6$   $��50,64,34.556,18.667q�69   $��2��6V   $��2��6c   � 5*   
   $��3��6   ���	#FF3CA8FF.+�5+      �� -	   5        $��checkBoxNearMode��5+   �   $��	Near Mode��6   $
��Auto��6,   $
��Left=�6:   $��Center=�6U   $��	0,12,10,8q�6p   $��Consolas)�6�   $��Normal$�6�   
�������6
  �� 5,      -
   5         ��Name$ 	enableAec��5,      $2��,Enable Echo Cancellation on Default Speakers��6*   
�������$��1092,12,10,8q�6t   {�.  $��Center=�6�   $��Center=�6�   $��Consolas)�6  $��Normal$�6#  5-      5.      5/      