using AForge.Genetic;
using Battlesnake.AI;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using Battlesnake.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Battlesnake.Train
{
    public class Training
    {
        public readonly GameStatusDTO Game;
        private readonly Random _rand;
        private GameObject[][] _grid;
        private GameState _gameState = GameState.PLAYING;

        public const int SNAKE_COUNT = 1;

        public Training(int height, int width)
        {
            Game = new GameStatusDTO
            {
                Board = new Board()
                {
                    Height = height,
                    Width = width
                },
                Game = new Game()
                {
                    Id = Guid.NewGuid().ToString(),
                    Ruleset = new Ruleset()
                    {
                        Name = "New game",
                        Settings = new RulesetSettings()
                        {
                            FoodSpawnChance = 100,
                            HazardDamagePerTurn = 1,
                            MinimumFood = 1
                        },
                        Version = "V1"
                    }
                },
                Turn = 0
            };
            _rand = new Random();
            InitalizeGame(new List<Snake>());
        }

        public Training(int height, int width, List<Snake> snakes)
        {
            Game = new GameStatusDTO
            {
                Board = new Board()
                {
                    Height = height,
                    Width = width
                },
                Game = new Game()
                {
                    Id = Guid.NewGuid().ToString(),
                    Ruleset = new Ruleset()
                    {
                        Name = "New game",
                        Settings = new RulesetSettings()
                        {
                            FoodSpawnChance = 100,
                            HazardDamagePerTurn = 1,
                            MinimumFood = 1
                        },
                        Version = "V1"
                    }
                },
                Turn = 0
            };
            _rand = new Random();
            InitalizeGame(snakes);
        }

        public void Play()
        {
            while (_gameState == GameState.PLAYING)
            {
                if (IsGameOver())
                {
                    _gameState = GameState.DONE;
                    break;
                }

                AIMove();
                SnakeCollsion();
                FoodCollision();
                Move();
                Turn();

                if (_gameState == GameState.REPLAY)
                {
                    Print();
                }
            }
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Game.Board.Height && y >= 0 && y < Game.Board.Width;
        }

        private bool IsGameOver()
        {
            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive && s.Health <= 0 || !IsInBounds(s.Head.X, s.Head.Y)))
            {
                snake.IsAlive = false;
                ClearFromGrid(snake);
            }

            return !Game.Board.Snakes.Any(s => s.IsAlive);
        }

        private void Move()
        {
            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive))
            {
                int prevX = snake.Head.X, prevY = snake.Head.Y;
                switch (snake.Direction)
                {
                    case Direction.UP:
                        snake.Head.X -= 1;
                        break;
                    case Direction.DOWN:
                        snake.Head.X += 1;
                        break;
                    case Direction.LEFT:
                        snake.Head.Y -= 1;
                        break;
                    case Direction.RIGHT:
                        snake.Head.Y += 1;
                        break;
                    default:
                        break;
                }

                if (Game.Turn < 3)
                {
                    if (snake.Length == 1)
                    {
                        snake.Body.Add(new Point()
                        {
                            X = prevX,
                            Y = prevY
                        });
                    }
                    else
                    {
                        snake.Body.Add(new Point()
                        {
                            X = snake.Body.Last().X,
                            Y = snake.Body.Last().Y
                        });
                    }
                    snake.Length++;
                }

                for (int i = 0; i < snake.Body.Count; i++)
                {
                    _grid[prevX][prevY] = GameObject.BODY;
                    var temp = snake.Body[i];
                    snake.Body[i] = new Point() { X = prevX, Y = prevY };
                    prevX = temp.X;
                    prevY = temp.Y;
                }

                if (IsInBounds(snake.Head.X, snake.Head.Y))
                    _grid[snake.Head.X][snake.Head.Y] = GameObject.HEAD;
                _grid[prevX][prevY] = GameObject.FLOOR;
            }
        }

        private void AIMove()
        {
            List<(int x, int y)> dirs = new List<(int x, int y)>()
            {
                (0, -1), //Left
                (1, 0), //Down
                (0, 1), //Right
                (-1, 0) //Up
            };

            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive))
            {
                double[] inputs = new double[Constants.INPUTS_COUNT];
                for (int i = 0; i < dirs.Count; i++)
                {
                    (int x, int y) = dirs[i];
                    int dx = snake.Head.X + x;
                    int dy = snake.Head.Y + y;
                    double value = Convert.ToDouble(IsInBounds(dx, dy) && _grid[dx][dy] == GameObject.FLOOR || IsInBounds(dx, dy) && _grid[dx][dy] == GameObject.FOOD);// || _grid[dx][dy] == GameObject.HEAD);
                    inputs[i] = value;
                }

                var food = ClosestFood(snake.Head.X, snake.Head.Y);
                inputs[4] = Convert.ToDouble(food.Y < snake.Head.Y);
                inputs[5] = Convert.ToDouble(food.X > snake.Head.X);
                inputs[6] = Convert.ToDouble(food.Y > snake.Head.Y);
                inputs[7] = Convert.ToDouble(food.X < snake.Head.X);

                for (int i = 0; i < inputs.Length; i++)
                    inputs[i] = inputs[i] * 2 - 1;

                List<double> output = snake.Brain.Compute(inputs).ToList();
                double max = output.Max();
                int maxIndex = output.IndexOf(max);
                switch (maxIndex)
                {
                    case 0:
                        MoveUp(snake);
                        break;
                    case 1:
                        MoveDown(snake);
                        break;
                    case 2:
                        MoveLeft(snake);
                        break;
                    case 3:
                        MoveRight(snake);
                        break;
                    default:
                        throw new Exception("Invald index exception");
                }
            }
        }

        private void MoveLeft(Snake snake)
        {
            if (snake.Direction != Direction.RIGHT)
                snake.Direction = Direction.LEFT;
        }

        private void MoveRight(Snake snake)
        {
            if (snake.Direction != Direction.LEFT)
                snake.Direction = Direction.RIGHT;
        }

        private void MoveUp(Snake snake)
        {
            if (snake.Direction != Direction.DOWN)
                snake.Direction = Direction.UP;
        }

        private void MoveDown(Snake snake)
        {
            if (snake.Direction != Direction.UP)
                snake.Direction = Direction.DOWN;
        }

        private Point ClosestFood(int x, int y)
        {
            Point point = null;
            var maxDistance = int.MaxValue;
            foreach (var food in Game.Board.Food)
            {
                var distance = Math.Abs(food.X - x) + Math.Abs(food.Y - y);
                if (maxDistance > distance)
                {
                    maxDistance = distance;
                    point = food;
                }
            }
            return point;
        }

        private void SnakeCollsion()
        {
            var aliveSnakes = Game.Board.Snakes.Where(s => s.IsAlive);
            foreach (var me in aliveSnakes)
            {
                foreach (var other in aliveSnakes.Where(s => s.IsAlive))
                {
                    if (me.Id != other.Id && me.Head.X == other.Head.X && me.Head.Y == other.Head.Y && other.Length >= me.Length)
                    {
                        other.SnakesEaten += 1;
                        me.IsAlive = false;
                        ClearFromGrid(me);
                        break;
                    }
                    else if (other.Body.Any(b => other.IsAlive && b.X == me.Head.X && b.Y == me.Head.Y)) //Self and body collision
                    {
                        me.IsAlive = false;
                        ClearFromGrid(me);
                        break;
                    }
                }
            }
        }

        private void ClearFromGrid(Snake snake)
        {
            //_grid[snake.Head.X][snake.Head.Y] = GameObject.FLOOR;
            foreach (var point in snake.Body)
            {
                _grid[point.X][point.Y] = GameObject.FLOOR;
            }
        }

        private void FoodCollision()
        {
            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive))
            {
                Point point = Game.Board.Food.FirstOrDefault(food => food.X == snake.Head.X && food.Y == snake.Head.Y);
                if (point != null)
                {
                    _grid[point.X][point.Y] = GameObject.FLOOR;
                    snake.Body.Add(new Point()
                    {
                        X = point.X,
                        Y = point.Y
                    });
                    snake.Health = 100;
                    snake.Length += 1;
                    snake.Score += 1;
                    Game.Game.Ruleset.Settings.FoodSpawnChance = 100;
                    Game.Board.Food.Remove(point);
                    GenerateFood();
                }
            }
        }

        private void Turn()
        {
            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive))
            {
                snake.Health -= Game.Game.Ruleset.Settings.HazardDamagePerTurn;
                snake.Moves += 1;
            }
            Game.Turn += 1;
        }

        private void Print()
        {
            Console.Clear();
            for (int i = 0; i < _grid.Length + 2; i++)
                Console.Write("#");

            Console.WriteLine();
            for (int i = 0; i < _grid.Length; i++)
            {
                Console.Write("#");
                for (int j = 0; j < _grid[i].Length; j++)
                    Console.Write((char)_grid[i][j]);
                Console.Write("#");
                Console.WriteLine();
            }

            for (int i = 0; i < _grid.Length + 2; i++)
                Console.Write("#");

            Thread.Sleep(Constants.FPS);
        }

        private void InitalizeGame(List<Snake> snakes)
        {
            InitializeGrid();

            if (snakes.Count == 0)
            {
                for (int i = 0; i < SNAKE_COUNT; i++)
                {
                    var snake = GenerateSnake(null);
                    if (i == 0)
                    {
                        Game.You = snake;
                        Game.Board.Snakes.Add(snake);
                    }
                    else
                    {
                        Game.Board.Snakes.Add(snake);
                    }
                }
            }
            else
            {
                for (int i = 0; i < snakes.Count; i++)
                {
                    var current = snakes[i];
                    var snake = GenerateSnake(current.Brain);
                    if (i == 0)
                    {
                        Game.You = snake;
                        Game.Board.Snakes.Add(snake);
                    }
                    else
                    {
                        Game.Board.Snakes.Add(snake);
                    }
                }
            }

            for (int i = 0; i < _grid.Length; i++)
            {
                for (int j = 0; j < _grid[i].Length; j++)
                {
                    if (Game.Board.Snakes.Any(snake => i == snake.Head.X && j == snake.Head.Y))
                        _grid[i][j] = GameObject.HEAD;
                }
            }

            GenerateFood();
        }

        private void InitializeGrid()
        {
            _grid = new GameObject[Game.Board.Height][];
            for (int i = 0; i < _grid.Length; i++)
            {
                _grid[i] = new GameObject[Game.Board.Width];
                for (int j = 0; j < _grid[i].Length; j++)
                    _grid[i][j] = GameObject.FLOOR;
            }
        }

        private void GenerateFood()
        {
            int spawnChance = Game.Game.Ruleset.Settings.FoodSpawnChance;
            if (spawnChance >= _rand.Next(0, spawnChance) + 1)
            {
                int x = _rand.Next(Game.Board.Height);
                int y = _rand.Next(Game.Board.Width);
                while (_grid[x][y] != GameObject.FLOOR)
                {
                    x = _rand.Next(Game.Board.Height);
                    y = _rand.Next(Game.Board.Width);
                }
                _grid[x][y] = GameObject.FOOD;
                Game.Board.Food.Add(new Point() { X = x, Y = y });
                Game.Game.Ruleset.Settings.FoodSpawnChance = 15;
            }
        }

        private Snake GenerateSnake(NeuralNetwork brain)
        {
            int x = _rand.Next(Game.Board.Height);
            int y = _rand.Next(Game.Board.Width);
            while (_grid[x][y] != GameObject.FLOOR)
            {
                x = _rand.Next(Game.Board.Height);
                y = _rand.Next(Game.Board.Width);
            }
            Snake snake = new Snake()
            {
                Head = new Point()
                {
                    X = x,
                    Y = y,
                },
                Health = 500,
                Id = Guid.NewGuid().ToString(),
                Length = 1,
                IsAlive = true
            };
            if (brain != null) snake.Brain = brain;
            snake.Direction = (Direction)_rand.Next(0, 4);
            return snake;
        }
    }
}
