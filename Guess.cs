using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TriviaGame
{
    public class Guess
    {
        public int GueID { get; set; }
        public int GueAnsID { get; set; }
        public Nullable<int> GueGamID { get; set; }

        public virtual Answer Answer { get; set; }
        public virtual Game Game { get; set; }
    }
}
