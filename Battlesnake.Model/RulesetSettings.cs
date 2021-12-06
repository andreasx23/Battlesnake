using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Model
{
    public class RulesetSettings
    {
        public int FoodSpawnChance { get; set; } = 100;
        public int MinimumFood { get; set; } = 1;
        public int HazardDamagePerTurn { get; set; } = 1;
        //Royale mode
        public int ShrinkEveryNTurns { get; set; } = int.MaxValue;
        //Squad mode
        public bool AllowBodyCollisions { get; set; } = false;
        public bool SharedElimination { get; set; } = false;
        public bool SharedHealth { get; set; } = false;
        public bool SharedLength { get; set; } = false;
    }
}
