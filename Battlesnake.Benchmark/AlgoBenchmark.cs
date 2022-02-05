using Battlesnake.Algorithm;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Benchmark
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class AlgoBenchmark
    {
        private static GameStatusDTO _state;

        [GlobalSetup]
        public void GlobalSetup()
        {
            string json = GameStateDuel.PinUpAgainstWall;
            _state = JsonConvert.DeserializeObject<GameStatusDTO>(json);
        }

        [Benchmark]
        public void MaxNBenchmark()
        {
            Algo alg = new(_state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
        }
    }
}
