using AForge.Genetic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Algorithm.GeneticAlgorithmTest
{
    public class ConstantFitness : IFitnessFunction
    {
        public double Evaluate(IChromosome chromosome)
        {
            ConstantsChromosome chromo = (ConstantsChromosome)chromosome;
            return chromo.GetFitness();
        }
    }
}
