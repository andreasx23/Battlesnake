using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Model
{
    public class Game
    {
        public string Id { get; set; }
        public Ruleset Ruleset { get; set; }
        public int Timeout { get; set; } //In miliseconds
        public string Source { get; set; }
    }
}
