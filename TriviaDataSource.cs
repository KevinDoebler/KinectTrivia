using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TriviaGame
{
    class TriviaDataSource
    {
        private List<Question> questions;

        public TriviaDataSource()
        {
            PopulateQuestions();
        }

        public Question GetQuestion()
        {
            return questions[0];
        }

        public List<Question> GetQuestions()
        {
            TriviaDataSource trivia = new TriviaDataSource();
            questions = new List<Question>();

            Question q = new Question();
            q.QueQuestion = "What venue lost power during the 3rd quarter of Super Bowl XLVII between the San Francisco 49ers and Baltimore Ravens? What venue lost power during the 3rd quarter of Super Bowl XLVII between the San Francisco 49ers and Baltimore Ravens?";
            q.QueID = 1;

            // 
            Answer a11 = new Answer();
            a11.AnsQueID = 1;
            a11.AnsID = 1;
            a11.AnsText = "Mercedes Benz Superdome";
            a11.AnsImagePath = "superdome.jpg";

            Answer a12 = new Answer();
            a12.AnsQueID = 1;
            a12.AnsID = 2;
            a12.AnsText = "Pontiac Silverdome";
            a12.AnsImagePath = "superdome.jpg";

            Answer a13 = new Answer();
            a13.AnsQueID = 1;
            a13.AnsID = 3;
            a13.AnsText = "Little Caesars Arena";
            a13.AnsImagePath = "superdome.jpg";

            //
            Answer a21 = new Answer();
            a21.AnsQueID = 2;
            a21.AnsID = 4;
            a21.AnsText = "Mercedes Benz Superdome";
            a21.AnsImagePath = "danmarino.jpg";

            Answer a22 = new Answer();
            a22.AnsQueID = 2;
            a22.AnsID = 5;
            a22.AnsText = "Pontiac Silverdome";
            a22.AnsImagePath = "elimanning.jpg";

            Answer a23 = new Answer();
            a23.AnsQueID = 2;
            a23.AnsID = 6;
            a23.AnsText = "Little Caesars Arena";
            a23.AnsImagePath = "joetheismann.jpg";

            //
            Answer a31 = new Answer();
            a31.AnsQueID = 3;
            a31.AnsID = 7;
            a31.AnsText = "Mercedes Benz Superdome";
            a31.AnsImagePath = "danmarino.jpg";

            Answer a32 = new Answer();
            a32.AnsQueID = 3;
            a32.AnsID = 8;
            a32.AnsText = "Pontiac Silverdome";
            a32.AnsImagePath = "elimanning.jpg";

            Answer a33 = new Answer();
            a33.AnsQueID = 3;
            a33.AnsID = 9;
            a33.AnsText = "Little Caesars Arena";
            a33.AnsImagePath = "joetheismann.jpg";

            List<Answer> answers = new List<Answer>();
            answers.Add(a11);
            answers.Add(a12);
            answers.Add(a13);

            answers.Add(a21);
            answers.Add(a22);
            answers.Add(a23);

            answers.Add(a31);
            answers.Add(a32);
            answers.Add(a33);

            q.Answers = answers;

            questions.Add(q);

            return questions;
        }



        private void PopulateQuestions()
        {
            //questions = new List<Question>();
            //Question questionInstance = new Question();

            //questionInstance.questionText = "What venue lost power during the 3rd quarter of Super Bowl XLVII between the San Francisco 49ers and Baltimore Ravens?";
            //var answers = new string[] { "Specifications", "Mercedes Benz Superdome", "Superdome", "Super Dome", "New Orleans" };            //var options = new string[] { "Lucas Oil Stadium", "Raymond James Stadium", "University of Phoenix Stadium", "Mercedes Benz Superdome" };            //questionInstance.hint = "The Big Easy";            //questionInstance.options = options;            //questionInstance.correctAnswers = answers;
            //questions.Add(questionInstance);

            //Question questionInstance2 = new Question();

            //questionInstance2.questionText = "The Cleveland Browns have the longest playoff drought, last appearing in the 2002 AFC wildcard game. Which team has the second longest drought? ";
            //var answers2 = new string[] { "Tampa Bay Buccaneers", "Tampa Bay", "Tampa", "Bucks", "Buccaneers" };

            //questionInstance2.answerDetail = "Tampa last appeared in 2007, with Coach Jon Gruden.";
            //questionInstance2.hint = "Gruden";

            //questionInstance2.correctAnswers = answers2;
            //questions.Add(questionInstance2);


        }
    }

    //public class Question
    //{
    //    public string questionText;
    //    public int questionDifficulty;
    //    public string[] options;
    //    public string hint;
    //    public string[] correctAnswers;
    //    public string answerDetail;


    //}

    public class Question
    {
        public Question()
        {
            this.Answers = new List<Answer>();
        }
        public int QueID { get; set; }
        public string QueQuestion { get; set; }
        public Nullable<int> QueDropSpeed { get; set; }
        public bool QueInactive { get; set; }
        public int QueDifficulty { get; set; }
        public virtual List<Answer> Answers { get; set; }
    }
}
