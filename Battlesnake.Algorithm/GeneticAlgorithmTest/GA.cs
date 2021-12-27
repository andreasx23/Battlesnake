using AForge.Genetic;
using Battlesnake.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Algorithm.GeneticAlgorithmTest
{
    public class GA
    {
        public static Snake FindBestSnake()
        {
            Console.WriteLine("Starting..");
            Snake snek = new();
            ConstantsChromosome chromo = new(snek);
            ConstantFitness fit = new();
            Population population = new(10, chromo, fit, new EliteSelection());
            population.MutationRate = 0.3d;
            //population.CrossoverRate = 0.2d;
            population.RunEpoch();
            double bestFitness = double.MinValue;
            int count = 0;
            while (count < 500)
            {
                population.RunEpoch();
                ConstantsChromosome currentBest = (ConstantsChromosome)population.BestChromosome;
                double fitness = currentBest.GetFitness();
                Console.WriteLine("Fitness: " + fitness);
                Console.WriteLine("FUTURE_UNCERTAINTY_FACOTR: " + currentBest.Snake.FUTURE_UNCERTAINTY_FACOTR);
                Console.WriteLine("AGGRESSION_VALUE: " + currentBest.Snake.AGGRESSION_VALUE);
                Console.WriteLine("MY_FOOD_VALUE: " + currentBest.Snake.MY_FOOD_VALUE);
                Console.WriteLine("OTHER_FOOD_VALUE: " + currentBest.Snake.OTHER_FOOD_VALUE);
                Console.WriteLine("MY_FLOODFILL_VALUE: " + currentBest.Snake.MY_FLOODFILL_VALUE);
                Console.WriteLine("OTHER_FLOODFILL_VALUE: " + currentBest.Snake.OTHER_FLOODFILL_VALUE);
                Console.WriteLine("VORONOI_VALUE: " + currentBest.Snake.VORONOI_VALUE);
                Console.WriteLine("EDGE_VALUE_INNER: " + currentBest.Snake.EDGE_VALUE_INNER);
                Console.WriteLine("EDGE_VALUE_OUTER: " + currentBest.Snake.EDGE_VALUE_OUTER);
                Console.WriteLine("CENTER_VALUE_INNER: " + currentBest.Snake.CENTER_VALUE_INNER);
                Console.WriteLine("CENTER_VALUE_OUTER: " + currentBest.Snake.CENTER_VALUE_OUTER);
                Console.WriteLine();
                if (fitness > bestFitness)
                {
                    bestFitness = fitness;
                    count = 0;
                }
                else
                {
                    count++;
                }
            }
            ConstantsChromosome best = ((ConstantsChromosome)population.BestChromosome);
            return best.Snake;
        }
    }
}
