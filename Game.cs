using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TriviaGame
{
    using System;
    using System.Collections.Generic;

    public class Game
    {
        public Game()
        {
            this.Guesses = new HashSet<Guess>();
        }

        public int GamID { get; set; }
        public string GamPlayerName { get; set; }
        public Nullable<int> GamScore { get; set; }
        public Nullable<System.DateTime> GamDateTime { get; set; }
        public int GamDifficulty { get; set; }

        public virtual ICollection<Guess> Guesses { get; set; }
    }
}
