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
        private static GameStatusDTO DeserializeGameStatusDTO(string json) => JsonConvert.DeserializeObject<GameStatusDTO>(json);

        [Fact]
        public void NoHeadToHead()
        {
            string json = GameState.NoHeadToHead;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move != Direction.LEFT);
        }

        [Fact]
        public void MoveUpAgainstLeftWallForHeadToHead()
        {
            string json = GameState.MoveUpAgainstLeftWallForHeadToHead;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.UP);
        }

        [Fact]
        public void PinUpAgainstWall()
        {
            string json = GameState.PinUpAgainstWall;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.LEFT);
        }

        [Fact]
        public void AvoidHeadToHeadCenterFood()
        {
            string json = GameState.AvoidHeadToHeadCenterFood;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move != Direction.UP);
        }

        [Fact]
        public void DontEnterSmallCaves()
        {
            string json = GameState.DontEnterSmallCaves;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move != Direction.DOWN);
        }

        [Fact]
        public void TrapOpponentIntoCave()
        {
            string json = GameState.TrapOpponentIntoCavePart1;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.RIGHT);
            json = GameState.TrapOpponentIntoCavePart2;
            state = DeserializeGameStatusDTO(json);
            alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.RIGHT);
            json = GameState.TrapOpponentIntoCavePart3;
            state = DeserializeGameStatusDTO(json);
            alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.RIGHT);
            json = GameState.TrapOpponentIntoCavePart4;
            state = DeserializeGameStatusDTO(json);
            alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.RIGHT);
        }

        [Fact]
        public void AttemptPossibleCornorTrapLongOpponent()
        {
            string json = GameState.AttemptPossibleCornorTrapLongOpponentPart1;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.UP);
            json = GameState.AttemptPossibleCornorTrapLongOpponentPart2;
            state = DeserializeGameStatusDTO(json);
            alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.RIGHT);
        }

        [Fact]
        public void DrawIsBetterThanCertainDeath()
        {
            string json = GameState.DrawIsBetterThanCertainDeath;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove(state.You);
            Assert.True(move == Direction.LEFT);
        }
    }
}
