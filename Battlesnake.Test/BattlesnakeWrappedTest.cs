using Battlesnake.Algorithm;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Battlesnake.Test
{
    public class BattlesnakeWrappedTest
    {
        /*
         * https://nettogrof.github.io/battle-snake-board-generator/
         * https://play.battlesnake.com/u/bwuk/bwukmaxi/
         */
        private static GameStatusDTO DeserializeGameStatusDTO(string json) => JsonConvert.DeserializeObject<GameStatusDTO>(json);

        [Fact]
        public void TrapWildHeartInACave()
        {
            string json = GameStateWrapped.TrapWildHeartInACave;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.RIGHT, "We didn't go right and therefor didn't trap Wild Heart");
        }

        [Fact]
        public void DontEnterACave()
        {
            string json = GameStateWrapped.DontEnterACave;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.RIGHT, "We didn't go right and therefor we entered a cave with certain death");
        }
    }
}
