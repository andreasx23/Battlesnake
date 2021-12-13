using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Algorithm
{
    public class HeuristicConstants
    {
        //MINIMAX VALUES
        public const int MINIMAX_DEPTH = 8; //Used to be 10 without voronoi
        public const int MAX_HEALTH = 100;
        public const double MAX_SNAKE_LENGTH = 30d;

        //HEURISTIC
        public const double FUTURE_UNCERTAINTY_FACOTR = 0.87d;

        //AGGRESSION
        public const double AGGRESSION_VALUE = 7.5d;
        public const double CORNOR_AGGRESSION_VALUE = 2.5d;

        //FOOD
        public const double MY_FOOD_VALUE = 50d;
        public const double OTHER_FOOD_VALUE = 25d;

        //FLOODFILL
        public const double MY_FLOODFILL_VALUE = 1d;
        public const double OTHER_FLOODFILL_VALUE = 0.5d;
        public const double FLOODFILL_MAX = 100d;
        public const double FLOODFILL_MIN = 25d;
        public const double SAFE_CAVERN_SIZE = 1.8d;

        //VORONOI
        public const double VORONOI_VALUE = 1d;

        //EDGE
        public const double EDGE_VALUE_INNER = 25d;
        public const double EDGE_VALUE_OUTER = 12.5d;
    }
}
