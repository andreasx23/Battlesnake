using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Model
{
    public class Game
    {
        public string Id { get; set; } = "Default";
        public Ruleset Ruleset { get; set; } = new();
        public int Timeout { get; set; } = 500; //In miliseconds
        public string Source { get; set; } = "Default";
    }
}
