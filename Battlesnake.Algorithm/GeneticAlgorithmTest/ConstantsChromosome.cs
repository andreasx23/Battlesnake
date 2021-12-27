using AForge.Genetic;
using Battlesnake.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Algorithm.GeneticAlgorithmTest
{
    public class ConstantsChromosome : IChromosome
    {
        private const int TOTAL_AMOUNT_OF_GAMES = 10;
        private readonly Random _rand;
        public Snake Snake { get; set; }
        public int _wins = 0;
        
        public ConstantsChromosome(Snake snake)
        {
            _rand = new Random();
            Snake = snake;
            _wins = 0;

            //COMMENT FOR IT TO WORK -- STILL BUGGED
            //Train train = new(11, 11, Snake);
            //if (train.Play()) _wins++;

            //Parallel.For(0, TOTAL_AMOUNT_OF_GAMES, i =>
            //{
            //    Train train = new(11, 11, Snake);
            //    //if (train.Play()) Snake.Wins++;
            //    if (train.Play()) _wins++;
            //});
        }

        public double Fitness
        {
            get
            {
                return GetFitness();
            }
        }

        public double GetFitness()
        {
            return _wins / TOTAL_AMOUNT_OF_GAMES;
        }

        public IChromosome Clone()
        {
            Snake snake = Snake.Clone();
            ConstantsChromosome clone = new(snake);
            return clone;
        }

        public int CompareTo(object obj)
        {
            ConstantsChromosome other = (ConstantsChromosome)obj;
            return Fitness - other.Fitness > 0 ? -1 : 1;
        }

        public IChromosome CreateNew()
        {
            Snake snake = new();
            ConstantsChromosome newChromosome = new(snake);
            return newChromosome;
        }

        public void Crossover(IChromosome pair)
        {
            ConstantsChromosome other = (ConstantsChromosome)pair;
            //FUTURE_UNCERTAINTY_FACOTR
            if (TakeMine())
                other.Snake.FUTURE_UNCERTAINTY_FACOTR = Snake.FUTURE_UNCERTAINTY_FACOTR;
            else
                Snake.FUTURE_UNCERTAINTY_FACOTR = other.Snake.FUTURE_UNCERTAINTY_FACOTR;

            //AGGRESSION_VALUE
            if (TakeMine())
                other.Snake.AGGRESSION_VALUE = Snake.AGGRESSION_VALUE;
            else
                Snake.AGGRESSION_VALUE = other.Snake.AGGRESSION_VALUE;

            //MY_FOOD_VALUE
            if (TakeMine())
                other.Snake.MY_FOOD_VALUE = Snake.MY_FOOD_VALUE;
            else
                Snake.MY_FOOD_VALUE = other.Snake.MY_FOOD_VALUE;

            //OTHER_FOOD_VALUE
            if (TakeMine())
                other.Snake.OTHER_FOOD_VALUE = Snake.OTHER_FOOD_VALUE;
            else
                Snake.OTHER_FOOD_VALUE = other.Snake.OTHER_FOOD_VALUE;

            //MY_FLOODFILL_VALUE
            if (TakeMine())
                other.Snake.MY_FLOODFILL_VALUE = Snake.MY_FLOODFILL_VALUE;
            else
                Snake.MY_FLOODFILL_VALUE = other.Snake.MY_FLOODFILL_VALUE;

            //OTHER_FLOODFILL_VALUE
            if (TakeMine())
                other.Snake.OTHER_FLOODFILL_VALUE = Snake.OTHER_FLOODFILL_VALUE;
            else
                Snake.OTHER_FLOODFILL_VALUE = other.Snake.OTHER_FLOODFILL_VALUE;

            //VORONOI_VALUE
            if (TakeMine())
                other.Snake.VORONOI_VALUE = Snake.VORONOI_VALUE;
            else
                Snake.VORONOI_VALUE = other.Snake.VORONOI_VALUE;

            //EDGE_VALUE_INNER
            if (TakeMine())
                other.Snake.EDGE_VALUE_INNER = Snake.EDGE_VALUE_INNER;
            else
                Snake.EDGE_VALUE_INNER = other.Snake.EDGE_VALUE_INNER;

            //EDGE_VALUE_OUTER
            if (TakeMine())
                other.Snake.EDGE_VALUE_OUTER = Snake.EDGE_VALUE_OUTER;
            else
                Snake.EDGE_VALUE_OUTER = other.Snake.EDGE_VALUE_OUTER;

            //CENTER_VALUE_INNER
            if (TakeMine())
                other.Snake.CENTER_VALUE_INNER = Snake.CENTER_VALUE_INNER;
            else
                Snake.CENTER_VALUE_INNER = other.Snake.CENTER_VALUE_INNER;

            //CENTER_VALUE_OUTER
            if (TakeMine())
                other.Snake.CENTER_VALUE_OUTER = Snake.CENTER_VALUE_OUTER;
            else
                Snake.CENTER_VALUE_OUTER = other.Snake.CENTER_VALUE_OUTER;
        }

        public void Evaluate(IFitnessFunction function)
        {
            ConstantFitness fitness = (ConstantFitness)function;
            fitness.Evaluate(this);
        }

        public void Generate()
        {
            throw new NotImplementedException();
        }

        public void Mutate()
        {
            //FUTURE_UNCERTAINTY_FACOTR
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.FUTURE_UNCERTAINTY_FACOTR += _rand.NextDouble() / 8;
                else
                    Snake.FUTURE_UNCERTAINTY_FACOTR -= _rand.NextDouble() / 8;
            }

            //AGGRESSION_VALUE
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.AGGRESSION_VALUE += _rand.NextDouble();
                else
                    Snake.AGGRESSION_VALUE -= _rand.NextDouble();
            }

            //MY_FOOD_VALUE
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.MY_FOOD_VALUE += _rand.NextDouble();
                else
                    Snake.MY_FOOD_VALUE -= _rand.NextDouble();
            }

            //OTHER_FOOD_VALUE
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.OTHER_FOOD_VALUE += _rand.NextDouble() / 2;
                else
                    Snake.OTHER_FOOD_VALUE -= _rand.NextDouble() / 2;
            }

            //MY_FLOODFILL_VALUE
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.MY_FLOODFILL_VALUE += _rand.NextDouble() / 12;
                else
                    Snake.MY_FLOODFILL_VALUE -= _rand.NextDouble() / 12;
            }

            //OTHER_FLOODFILL_VALUE
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.OTHER_FLOODFILL_VALUE += _rand.NextDouble() / 24;
                else
                    Snake.OTHER_FLOODFILL_VALUE -= _rand.NextDouble() / 24;
            }

            //VORONOI_VALUE
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.VORONOI_VALUE += _rand.NextDouble() / 10;
                else
                    Snake.VORONOI_VALUE -= _rand.NextDouble() / 10;
            }

            //EDGE_VALUE_INNER
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.EDGE_VALUE_INNER += _rand.NextDouble() / 3;
                else
                    Snake.EDGE_VALUE_INNER -= _rand.NextDouble() / 3;
            }

            //EDGE_VALUE_OUTER
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.EDGE_VALUE_OUTER += _rand.NextDouble() / 6;
                else
                    Snake.EDGE_VALUE_OUTER -= _rand.NextDouble() / 6;
            }

            //CENTER_VALUE_INNER
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.CENTER_VALUE_INNER += _rand.NextDouble() / 2;
                else
                    Snake.CENTER_VALUE_INNER -= _rand.NextDouble() / 2;
            }

            //CENTER_VALUE_OUTER
            if (ShouldMutate())
            {
                if (TakeMine())
                    Snake.CENTER_VALUE_OUTER += _rand.NextDouble() / 4;
                else
                    Snake.CENTER_VALUE_OUTER -= _rand.NextDouble() / 4;
            }
        }

        private bool TakeMine()
        {
            return _rand.Next(0, 100) + 1 <= 50; //50% chance to select me else other
        }

        private bool ShouldMutate()
        {
            return _rand.Next(0, 100) + 1 <= 25; //25% chance to mutate this value
        }
    }
}
