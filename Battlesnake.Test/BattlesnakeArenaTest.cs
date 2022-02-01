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
    public class BattlesnakeArenaTest
    {
        /*
         * https://nettogrof.github.io/battle-snake-board-generator/
         * https://play.battlesnake.com/u/bwuk/bwukmaxi/
         */
        private static GameStatusDTO DeserializeGameStatusDTO(string json) => JsonConvert.DeserializeObject<GameStatusDTO>(json);

        [Fact]
        public void SidewinderJustAteDontMoveRightAndDownIsPossibleDraw()
        {
            string json = GameStateArena.SidewinderJustAteDontMoveRightAndDownIsPossibleDraw;
            GameStatusDTO state = DeserializeGameStatusDTO(json);
            Algo alg = new(state, Direction.NO_MOVE, Stopwatch.StartNew());
            Direction move = alg.CalculateNextMove();
            Assert.True(move == Direction.UP, "Snake went either down or right so game was lost");
        }
    }
}
