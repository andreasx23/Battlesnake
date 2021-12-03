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

namespace Battlesnake.Train
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AlogoRunLocal local = new(11, 11, 2);
            local.Play();

            //Stopwatch watch = Stopwatch.StartNew();
            //NeuralNetwork bestBrain = null; //NeuralNetwork.LoadNetwork();
            //int bestScore = 0;
            //double bestFitness = 0;
            //int generation = 1;
            //const int POPULATION_SIZE = 5000;

            //int size = 7;
            //List<Training> games = new();
            //for (int i = 0; i < POPULATION_SIZE; i++)
            //{
            //    var train = new Training(size, size);
            //    games.Add(train);
            //}

            //while (true)
            //{
            //    for (int i = 0; i < POPULATION_SIZE; i++)
            //    {
            //        games[i].Play();
            //    }

            //    double totalFitnessScoreThisGeneration = 0;
            //    double bestFitnessThisGeneration = -1;
            //    long totalScoreThisGeneration = 0;
            //    int bestScoreThisGeneration = -1;
            //    int index = -1;
            //    for (int i = 0; i < POPULATION_SIZE; i++)
            //    {
            //        int score = games[i].Game.Board.Snakes.Max(s => s.Score);
            //        double fitnessScore = games[i].Game.Board.Snakes.Max(s => s.Fitness);

            //        totalScoreThisGeneration += score;
            //        totalFitnessScoreThisGeneration += fitnessScore;

            //        if (score > bestScoreThisGeneration)
            //        {
            //            bestScoreThisGeneration = score;
            //        }

            //        if (fitnessScore > bestFitnessThisGeneration)
            //        {
            //            index = i;
            //            bestFitnessThisGeneration = fitnessScore;
            //        }
            //    }

            //    Console.WriteLine($"Currently using neurons: {Constants.NEURONS}");
            //    Console.WriteLine($"Population size: {POPULATION_SIZE}");
            //    Console.WriteLine($"Generation: {generation}");
            //    Console.WriteLine($"Total games played this session: {generation * POPULATION_SIZE}");
            //    Console.WriteLine($"Been running for: {watch.Elapsed}");
            //    Console.WriteLine($"Best overall fitness-score: {bestFitness}");
            //    Console.WriteLine($"Best overall score: {bestScore}");
            //    Console.WriteLine($"Best fitness-score this generation: {bestFitnessThisGeneration}");
            //    Console.WriteLine($"Average fitness-score this generation: {totalFitnessScoreThisGeneration / POPULATION_SIZE}");
            //    Console.WriteLine($"Best score this generation: {bestScoreThisGeneration}");
            //    Console.WriteLine($"Average score this generation: {totalScoreThisGeneration / POPULATION_SIZE}");

            //    //if (bestBrain == null || bestScoreThisGeneration > bestScore)
            //    //{
            //    //    Console.WriteLine($"Winner winner chicken dinner - new best score found! {bestScoreThisGeneration}");
            //    //    bestBrain = (NeuralNetwork)games[index].Game.Board.Snakes.First(s => s.Score == bestScoreThisGeneration).Brain.Clone();
            //    //    bestScore = bestScoreThisGeneration;
            //    //    //bestFitness = BigInteger.Max(bestFitness, bestFitnessThisGeneration);
            //    //    bestBrain.SaveNetwork(size, Training.SNAKE_COUNT, bestScore);
            //    //}
            //    //else 

            //    if (bestFitnessThisGeneration > bestFitness)
            //    {
            //        Console.WriteLine($"Winner winner chicken dinner - new best fitness-score found! {bestFitnessThisGeneration}");
            //        bestBrain = (NeuralNetwork)games[index].Game.Board.Snakes.First(s => s.Fitness == bestFitnessThisGeneration).Brain.Clone();
            //        bestScore = Math.Max(bestScore, bestScoreThisGeneration);
            //        bestFitness = bestFitnessThisGeneration;
            //        bestBrain.Fitness = bestFitnessThisGeneration;
            //        //bestBrain.SaveNetwork(size, Training.SNAKE_COUNT, bestFitness);
            //    }

            //    Console.WriteLine();

            //    for (int i = 0; i < POPULATION_SIZE; i++)
            //    {
            //        foreach (var snake in games[i].Game.Board.Snakes)
            //        {
            //            NeuralNetwork child = bestBrain.Breed(snake.Brain);
            //            child.Mutate();
            //            snake.Brain = child;
            //        }
            //        games[i] = new Training(size, size, games[i].Game.Board.Snakes);
            //    }
            //    generation++;
            //}
        }
    }
}
