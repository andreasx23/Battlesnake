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
        public Point Head { get; set; }
        public List<Point> Body { get; set; } = new List<Point>();
        public string Latency { get; set; }
        public int Length { get; set; }
        public string Shout { get; set; }
        public string Squad { get; set; }

        //Custom values
        [IgnoreDataMember]
        public bool IsAlive { get; set; } = true;
        [IgnoreDataMember]
        public Direction Direction { get; set; }
        [IgnoreDataMember]
        public int Score { get; set; } = 0;
        [IgnoreDataMember]
        public int Moves { get; set; } = 0;
        [IgnoreDataMember]
        public int SnakesEaten { get; set; } = 0;
        [IgnoreDataMember]
        public NeuralNetwork Brain { get; set; } = new NeuralNetwork(new BipolarSigmoidFunction(Constants.ALPHA), Constants.INPUTS_COUNT, Constants.NEURONS, Constants.OUTPUT_COUNT);


        public Snake Clone()
        {
            List<Point> body = new();
            foreach (var b in Body)
                body.Add(new Point() { X = b.X, Y = b.Y });
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
                //IsAlive = IsAlive,
                //Direction = Direction,
                //Score = Score,
                //Moves = Moves,
                //SnakesEaten = SnakesEaten,
                //Brain = Brain
            };
        }
    }
}
