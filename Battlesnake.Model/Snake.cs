using AForge.Genetic;
using AForge.Neuro;
using Battlesnake.AI;
using Battlesnake.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Model
{
    public class Snake
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Health { get; set; }
        public Point Head { get; set; } = new Point() { X = 0, Y = 0 };
        public List<Point> Body { get; set; } = new List<Point>();
        public string Latency { get; set; }
        public int Length { get; set; }
        public string Shout { get; set; }
        public string Squad { get; set; }
        public bool IsAlive => Health > 0;

        //Custom values
        //[IgnoreDataMember]
        //public bool IsAlive { get; set; } = true;
        [IgnoreDataMember]
        public Direction Direction { get; set; }
        [IgnoreDataMember]
        public int Score { get; set; } = 0;
        [IgnoreDataMember]
        public int Moves { get; set; } = 0;
        [IgnoreDataMember]
        public int SnakesEaten { get; set; } = 0;
        //Genetic algorithm testing
        [IgnoreDataMember]
        public int Wins { get; set; } = 0;
        //HEURISTIC
        [IgnoreDataMember]
        public double FUTURE_UNCERTAINTY_FACOTR { get; set; } = 0.87d;
        //AGGRESSION
        [IgnoreDataMember]
        public double AGGRESSION_VALUE { get; set; } = 7.5d;
        //FOOD
        [IgnoreDataMember]
        public double MY_FOOD_VALUE { get; set; } = 50d;
        [IgnoreDataMember]
        public double OTHER_FOOD_VALUE { get; set; } = 25d;
        //FLOODFILL
        [IgnoreDataMember]
        public double MY_FLOODFILL_VALUE { get; set; } = 1d;
        [IgnoreDataMember]
        public double OTHER_FLOODFILL_VALUE { get; set; } = 0.5d;
        //VORONOI
        [IgnoreDataMember]
        public double VORONOI_VALUE { get; set; } = 0.60983d;
        //EDGE
        [IgnoreDataMember]
        public double EDGE_VALUE_INNER { get; set; } = 25d;
        [IgnoreDataMember]
        public double EDGE_VALUE_OUTER { get; set; } = 12.5d;
        //CENTER
        [IgnoreDataMember]
        public double CENTER_VALUE_INNER { get; set; } = 35d;
        [IgnoreDataMember]
        public double CENTER_VALUE_OUTER { get; set; } = 17.5d;

        public Snake Clone()
        {
            List<Point> body = new();
            for (int i = 0; i < Body.Count; i++)
            {
                Point b = Body[i];
                body.Add(new Point() { X = b.X, Y = b.Y });
            }
            return new Snake()
            {
                Id = Id,
                Name = Name,
                Health = Health,
                Head = new Point() { X = Head.X, Y = Head.Y },
                Body = body,
                Latency = Latency,
                Length = Length,
                Shout = Shout,
                Squad = Squad,
                Wins = Wins,
                FUTURE_UNCERTAINTY_FACOTR = FUTURE_UNCERTAINTY_FACOTR,
                AGGRESSION_VALUE = AGGRESSION_VALUE,
                MY_FOOD_VALUE = MY_FOOD_VALUE,
                OTHER_FOOD_VALUE = OTHER_FOOD_VALUE,
                MY_FLOODFILL_VALUE = MY_FLOODFILL_VALUE,
                OTHER_FLOODFILL_VALUE = OTHER_FLOODFILL_VALUE,
                VORONOI_VALUE = VORONOI_VALUE,
                EDGE_VALUE_INNER = EDGE_VALUE_INNER,
                EDGE_VALUE_OUTER = EDGE_VALUE_OUTER,
                CENTER_VALUE_INNER = CENTER_VALUE_INNER,
                CENTER_VALUE_OUTER = CENTER_VALUE_OUTER
            };
        }
    }
}
