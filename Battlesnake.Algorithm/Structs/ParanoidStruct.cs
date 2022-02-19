using Battlesnake.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Algorithm.Structs
{
    public struct ParanoidStruct
    {
        public PointStruct OldHead { get; set; }
        public PointStruct OldNeck { get; set; }
        public PointStruct OldTail { get; set; }
        public GameObject DestinationTile { get; set; }
        public int PrevHp { get; set; }
        public int PrevLength { get; set; }
        public bool HasSnakeEaten { get; set; }
        public bool HasCurrentSnakeEaten { get; set; }
        public int CurrentFoodCount { get; set; }
        public PointStruct NewHead { get; set; }
        public PointStruct NewTail { get; set; }
        public int Index { get; set; }
        public bool DidMove { get; set; }
    }
}
