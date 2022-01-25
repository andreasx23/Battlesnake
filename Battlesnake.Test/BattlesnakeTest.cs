using Battlesnake.Algorithm;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Xunit;

namespace Battlesnake.Test
{
    public class BattlesnakeTest
    {
        [Fact]
        public void NoHeadToHead()
        {
            string json = GameState.NoHeadToHead;
            GameStatusDTO state = GetGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move != Direction.LEFT);
        }

        [Fact]
        public void MoveUpAgainstLeftWallForHeadToHead()
        {
            string json = GameState.MoveUpAgainstLeftWallForHeadToHead;
            GameStatusDTO state = GetGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.UP);
        }

        private static GameStatusDTO GetGameStatusDTO(string json)
        {
            return JsonConvert.DeserializeObject<GameStatusDTO>(json);
        }
    }
}
