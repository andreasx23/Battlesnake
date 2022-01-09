using Battlesnake.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Algorithm.Structs
{
    public struct TransporationValue
    {
        public Direction Move { get; set; }
        public int MoveIndex { get; set; }
        public int Depth { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
    }
}
