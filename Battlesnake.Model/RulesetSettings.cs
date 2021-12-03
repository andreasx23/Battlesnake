using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Model
{
    public class RulesetSettings
    {
        public int FoodSpawnChance { get; set; }
        public int MinimumFood { get; set; }
        public int HazardDamagePerTurn { get; set; }
        //Royale mode
        public int ShrinkEveryNTurns { get; set; }
        //Squad mode
        public bool AllowBodyCollisions { get; set; }
        public bool SharedElimination { get; set; }
        public bool SharedHealth { get; set; }
        public bool SharedLength { get; set; }
    }
}
