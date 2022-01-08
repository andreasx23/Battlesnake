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

namespace Battlesnake.Algorithm
{
    public class AlogoRunLocal
    {
        private readonly GameStatusDTO _game;
        private readonly Random _rand;
        private GameObject[][] _grid;
        private GameState _gameState = GameState.PLAYING;
        //private readonly Algo _algo;
        private readonly int _snakeCount;
        private readonly List<Algo> _brains;

        public AlogoRunLocal(int height, int width, int snakeCount)
        {
            _snakeCount = snakeCount;
            _game = new GameStatusDTO
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
            _brains = new();
            InitalizeGame();
        }

        public void Play()
        {
            //Print();

            while (_gameState == GameState.PLAYING)
            {
                FoodCollision();
                SnakeCollsion();
                //_game.You.Direction = _algo.CalculateNextMove();
                AIMove();
                MoveBody();
                Turn();

                _game.Board.Snakes.RemoveAll(s => !s.IsAlive); //To handle multiple players on the board

                if (IsGameOver())
                {
                    _gameState = GameState.DONE;
                    break;
                }
                else if (_gameState == GameState.PLAYING)
                {
                    _brains.First().Print();
                    foreach (var s in _game.Board.Snakes)
                    {
                        Console.WriteLine($"{s.Id}: {s.Score}");
                    }
                }
            }
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _game.Board.Height && y >= 0 && y < _game.Board.Width;
        }

        private bool IsGameOver()
        {
            foreach (var snake in _game.Board.Snakes.Where(s => s.IsAlive && s.Health <= 0 || !IsInBounds(s.Head.X, s.Head.Y)))
            {
                snake.IsAlive = false;
                ClearFromGrid(snake);
            }
            return !_game.Board.Snakes.Any(s => s.IsAlive);
        }

        private void AIMove()
        {
            int index = 0;
            foreach (var me in _game.Board.Snakes)
            {
                if (me.IsAlive)
                {
                    me.Direction = _brains[index].CalculateNextMove(me);
                    index++;
                }
            }
        }

        private void MoveBody()
        {
            foreach (var snake in _game.Board.Snakes.Where(s => s.IsAlive))
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

                if (_game.Turn < 3)
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

        private void SnakeCollsion()
        {
            var aliveSnakes = _game.Board.Snakes.Where(s => s.IsAlive);
            foreach (var me in aliveSnakes)
            {
                foreach (var other in aliveSnakes.Where(s => s.IsAlive))
                {
                    if (me.Id != other.Id && me.Head.X == other.Head.X && me.Head.Y == other.Head.Y && other.Length > me.Length)
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
            foreach (var point in snake.Body)
            {
                _grid[point.X][point.Y] = GameObject.FLOOR;
            }
        }

        private void FoodCollision()
        {
            foreach (var snake in _game.Board.Snakes.Where(s => s.IsAlive))
            {
                Point point = _game.Board.Food.FirstOrDefault(food => food.X == snake.Head.X && food.Y == snake.Head.Y);
                if (point != null)
                {
                    snake.Body.Add(new Point()
                    {
                        X = snake.Body.Last().X,
                        Y = snake.Body.Last().Y
                    });
                    snake.Health = 100;
                    snake.Length += 1;
                    snake.Score += 1;
                    _game.Board.Food.Remove(point);
                    _game.Game.Ruleset.Settings.FoodSpawnChance = 100;
                }
            }

            GenerateFood();
        }

        private void Turn()
        {
            foreach (var snake in _game.Board.Snakes.Where(s => s.IsAlive))
            {
                snake.Health -= _game.Game.Ruleset.Settings.HazardDamagePerTurn;
                snake.Moves += 1;
            }
            _game.Turn += 1;
        }

        private void InitalizeGame()
        {
            InitializeGrid();

            for (int i = 0; i < _snakeCount; i++)
            {
                Snake snake = GenerateSnake();
                if (i == 0)
                    _game.You = snake;
                _game.Board.Snakes.Add(snake);
                _brains.Add(new Algo(_game, _grid));
            }

            for (int i = 0; i < _grid.Length; i++)
            {
                for (int j = 0; j < _grid[i].Length; j++)
                {
                    if (_game.Board.Snakes.Any(snake => i == snake.Head.X && j == snake.Head.Y))
                        _grid[i][j] = GameObject.HEAD;
                }
            }

            GenerateFood();
        }

        private void InitializeGrid()
        {
            _grid = new GameObject[_game.Board.Height][];
            for (int i = 0; i < _grid.Length; i++)
            {
                _grid[i] = new GameObject[_game.Board.Width];
                for (int j = 0; j < _grid[i].Length; j++)
                    _grid[i][j] = GameObject.FLOOR;
            }
        }

        private void GenerateFood()
        {
            if (_game.Game.Ruleset.Settings.FoodSpawnChance >= _rand.Next(0, 100) + 1) //1 - 100
            {
                int x = _rand.Next(_game.Board.Height);
                int y = _rand.Next(_game.Board.Width);
                while (_grid[x][y] != GameObject.FLOOR)
                {
                    x = _rand.Next(_game.Board.Height);
                    y = _rand.Next(_game.Board.Width);
                }
                _game.Board.Food.Add(new Point() { X = x, Y = y });
                _grid[x][y] = GameObject.FOOD;
                _game.Game.Ruleset.Settings.FoodSpawnChance = 15;
            }
        }

        private Snake GenerateSnake()
        {
            int x = _rand.Next(_game.Board.Height);
            int y = _rand.Next(_game.Board.Width);
            while (_grid[x][y] != GameObject.FLOOR)
            {
                x = _rand.Next(_game.Board.Height);
                y = _rand.Next(_game.Board.Width);
            }
            Snake snake = new()
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
            snake.Direction = (Direction)_rand.Next(0, 4);
            return snake;
        }
    }
}
