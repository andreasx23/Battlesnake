using BenchmarkDotNet.Running;
using System;

namespace Battlesnake.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<AlgoBenchmark>();
        }
    }
}
