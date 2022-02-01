using Battlesnake.Algorithm;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Xunit;

namespace Battlesnake.Test
{
    public class BattlesnakeDuelTest
    {
        /*
         * https://nettogrof.github.io/battle-snake-board-generator/
         * https://play.battlesnake.com/u/bwuk/bwukmaxi/
         */
        private static GameStatusDTO DeserializeGameStatusDTO(string json) => JsonConvert.DeserializeObject<GameStatusDTO>(json);

        [Fact]
        public void NoHeadToHead()
        {
            string json = GameStateDuel.NoHeadToHead;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move != Direction.LEFT);
        }

        [Fact]
        public void MoveUpAgainstLeftWallForHeadToHead()
        {
            string json = GameStateDuel.MoveUpAgainstLeftWallForHeadToHead;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.UP);
        }

        [Fact]
        public void PinUpAgainstWall()
        {
            string json = GameStateDuel.PinUpAgainstWall;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.LEFT);
        }

        [Fact]
        public void AvoidHeadToHeadCenterFood()
        {
            string json = GameStateDuel.AvoidHeadToHeadCenterFood;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move != Direction.UP);
        }

        [Fact]
        public void DontEnterSmallCaves()
        {
            string json = GameStateDuel.DontEnterSmallCaves;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move != Direction.DOWN);
        }

        [Fact]
        public void TrapOpponentIntoCave()
        {
            string json = GameStateDuel.TrapOpponentIntoCavePart1;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.RIGHT);
            json = GameStateDuel.TrapOpponentIntoCavePart2;
            state = DeserializeGameStatusDTO(json);
            alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            move = alg.CalculateNextMove();
            Assert.True(move == Direction.RIGHT);
            json = GameStateDuel.TrapOpponentIntoCavePart3;
            state = DeserializeGameStatusDTO(json);
            alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            move = alg.CalculateNextMove();
            Assert.True(move == Direction.RIGHT);
            json = GameStateDuel.TrapOpponentIntoCavePart4;
            state = DeserializeGameStatusDTO(json);
            alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            move = alg.CalculateNextMove();
            Assert.True(move == Direction.RIGHT);
        }

        [Fact]
        public void AttemptPossibleCornorTrapLongOpponent()
        {
            string json = GameStateDuel.AttemptPossibleCornorTrapLongOpponentPart1;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.UP);
            json = GameStateDuel.AttemptPossibleCornorTrapLongOpponentPart2;
            state = DeserializeGameStatusDTO(json);
            alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            move = alg.CalculateNextMove();
            Assert.True(move == Direction.RIGHT);
        }

        [Fact]
        public void DrawIsBetterThanCertainDeath()
        {
            string json = GameStateDuel.DrawIsBetterThanCertainDeath;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.LEFT);
        }

        [Fact]
        public void EatYoureHungryOneHpLeft()
        {
            string json = GameStateDuel.EatYoureHungryOneHpLeft;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.DOWN);
        }

        [Fact]
        public void PreferDrawWhenHungryEvenWithOneHpLeft()
        {
            string json = GameStateDuel.PreferDrawWhenHungryEvenWithOneHpLeft;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.DOWN);
        }

        [Fact]
        public void NeverRunIntoYourSelf()
        {
            string json = GameStateDuel.NeverRunIntoYourSelf;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.LEFT);
        }

        [Fact]
        public void NeverRunIntoWalls()
        {
            string json = GameStateDuel.NeverRunIntoWalls;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.RIGHT);
        }

        [Fact]
        public void OnlyApplyEatMoveOnNextTurn()
        {
            string json = GameStateDuel.OnlyApplyEatMoveOnNextTurn;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.LEFT);
        }
    }
}
