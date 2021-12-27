using Battlesnake.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using AForge.Genetic;
using AForge.Neuro;
using AForge.Neuro.Learning;
using Battlesnake.Algorithm;
using Battlesnake.Algorithm.GeneticAlgorithmTest;

namespace Battlesnake.Train
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //            public int Wins { get; set; } = 0;
            ////HEURISTIC
            //public double FUTURE_UNCERTAINTY_FACOTR { get; set; } = 0.87d;
            ////AGGRESSION
            //public double AGGRESSION_VALUE { get; set; } = 7.5d;
            ////FOOD
            //public double MY_FOOD_VALUE { get; set; } = 50d;
            //public double OTHER_FOOD_VALUE { get; set; } = 25d;
            ////FLOODFILL
            //public double MY_FLOODFILL_VALUE { get; set; } = 1d;
            //public double OTHER_FLOODFILL_VALUE { get; set; } = 0.5d;
            ////VORONOI
            //public double VORONOI_VALUE { get; set; } = 0.60983d;
            ////EDGE
            //public double EDGE_VALUE_INNER { get; set; } = 25d;
            //public double EDGE_VALUE_OUTER { get; set; } = 12.5d;
            ////CENTER
            //public double CENTER_VALUE_INNER { get; set; } = 35d;
            //public double CENTER_VALUE_OUTER { get; set; } = 17.5d;

            var snek = GA.FindBestSnake();
            Console.Clear();
            Console.WriteLine("BEST VALUES");
            Console.WriteLine(snek.FUTURE_UNCERTAINTY_FACOTR);
            Console.WriteLine(snek.AGGRESSION_VALUE);
            Console.WriteLine(snek.MY_FOOD_VALUE);
            Console.WriteLine(snek.OTHER_FOOD_VALUE);
            Console.WriteLine(snek.MY_FLOODFILL_VALUE);
            Console.WriteLine(snek.OTHER_FLOODFILL_VALUE);
            Console.WriteLine(snek.VORONOI_VALUE);
            Console.WriteLine(snek.EDGE_VALUE_INNER);
            Console.WriteLine(snek.EDGE_VALUE_OUTER);
            Console.WriteLine(snek.CENTER_VALUE_INNER);
            Console.WriteLine(snek.CENTER_VALUE_OUTER);

            //AlogoRunLocal local = new(11, 11, 2);
            //local.Play();
        }
    }
}
