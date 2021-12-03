using Battlesnake.AI;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using Battlesnake.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Battlesnake.API.Action
{
    public static class GameAction
    {
        private static readonly List<(int x, int y)> dirs = new()
        {
            (0, -1), //Left
            (-1, 0), //Down
            (0, 1), //Right
            (1, 0) //Up
        };

        public static Direction DoAIMove(GameStatusDTO game, NeuralNetwork brain, Direction direction)
        {
            double[] inputs = new double[Constants.INPUTS_COUNT];
            for (int i = 0; i < dirs.Count; i++)
            {
                (int x, int y) = dirs[i];
                int dx = game.You.Head.X + x;
                int dy = game.You.Head.Y + y;
                double value = Convert.ToDouble(IsFreeTile(game, dx, dy) || IsFoodTile(game, dx, dy));// || _grid[dx][dy] == GameObject.HEAD);
                inputs[i] = value * 2 - 1;
            }

            Point target = ClosestFood(game);
            if (target != null)
            {
                inputs[4] = Convert.ToDouble(target.Y < game.You.Head.Y) * 2 - 1;
                inputs[5] = Convert.ToDouble(target.X > game.You.Head.X) * 2 - 1;
                inputs[6] = Convert.ToDouble(target.Y > game.You.Head.Y) * 2 - 1;
                inputs[7] = Convert.ToDouble(target.X < game.You.Head.X) * 2 - 1;                
            }
            else
            {
                for (int i = 4; i < inputs.Length; i++)
                    inputs[i] = -1;
            }

            List<double> output = brain.Compute(inputs).ToList();
            double max = output.Max();
            int maxIndex = output.IndexOf(max);
            return maxIndex switch
            {
                0 => MoveUp(direction),
                1 => MoveDown(direction),
                2 => MoveLeft(direction),
                3 => MoveRight(direction),
                _ => throw new Exception("Invald index exception"),
            };
        }

        private static Direction MoveLeft(Direction direction)
        {
            if (direction != Direction.RIGHT)
                direction = Direction.LEFT;
            return direction;
        }

        private static Direction MoveRight(Direction direction)
        {
            if (direction != Direction.LEFT)
                direction = Direction.RIGHT;
            return direction;
        }

        private static Direction MoveUp(Direction direction)
        {
            if (direction != Direction.DOWN)
                direction = Direction.UP;
            return direction;
        }

        private static Direction MoveDown(Direction direction)
        {
            if (direction != Direction.UP)
                direction = Direction.DOWN;
            return direction;

        }

        private static Point ClosestFood(GameStatusDTO game)
        {
            Point point = null;
            int maxDistance = int.MaxValue;
            foreach (var food in game.Board.Food)
            {
                int distance = Math.Abs(food.X - game.You.Head.X) + Math.Abs(food.Y - game.You.Head.Y);
                if (maxDistance > distance)
                {
                    maxDistance = distance;
                    point = food;
                }
            }
            return point;
        }

        private static bool IsFoodTile(GameStatusDTO game, int x, int y)
        {
            return IsInBounds(game, x, y) && game.Board.Food.Any(p => p.X == x && p.Y == y);
        }

        private static bool IsFreeTile (GameStatusDTO game, int x, int y)
        {
            return IsInBounds(game, x, y) && !game.Board.Snakes.Any(s => s.Head.X == x && s.Head.Y == y || s.Body.Any(b => b.X == x && b.Y == y));
        }

        private static bool IsInBounds(GameStatusDTO game, int x, int y)
        {
            return x >= 0 && x < game.Board.Height && y >= 0 && y < game.Board.Width;
        }
    }
}
