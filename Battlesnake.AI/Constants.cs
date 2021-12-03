using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.AI
{
    public class Constants
    {
        //AI
        public const float ALPHA = 0.5f;
        public const int INPUTS_COUNT = 8;
        public static int NEURONS = 200;
        public const int OUTPUT_COUNT = 4;
        public const float MUTATION_CHANCE = 0.3f;
        public const int MAX_MUTATION_COUNT = 5;

        //GAME
        public const int FPS = 1; //Increase to like 50 if human is playing
    }
}
