using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TriviaGame
{
    public class Answer
    {
        public Answer()
        {
            this.Guesses = new HashSet<Guess>();
        }

        public int AnsID { get; set; }
        public int AnsQueID { get; set; }
        public string AnsImagePath { get; set; }
        public bool AnsCorrect { get; set; }
        public bool AnsInactive { get; set; }
        public string AnsText { get; set; }

        public virtual Question Question { get; set; }
        public virtual ICollection<Guess> Guesses { get; set; }
    }
}
