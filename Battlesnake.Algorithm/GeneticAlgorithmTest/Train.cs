//using Battlesnake.AI;
//using Battlesnake.DTOModel;
//using Battlesnake.Enum;
//using Battlesnake.Model;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Battlesnake.Algorithm.GeneticAlgorithmTest
//{
//    public class Train
//    {
//        /* Algo CONTRUCTOR!
//         * 
//         * public double FUTURE_UNCERTAINTY_FACOTR { get; set; } = 0.87d;
//        public double AGGRESSION_VALUE { get; set; } = 7.5d;
//        public double MY_FOOD_VALUE { get; set; } = 50d;
//        public double OTHER_FOOD_VALUE { get; set; } = 25d;
//        public double MY_FLOODFILL_VALUE { get; set; } = 1d;
//        public double OTHER_FLOODFILL_VALUE { get; set; } = 0.5d;
//        public double VORONOI_VALUE { get; set; } = 0.60983d;
//        public double EDGE_VALUE_INNER { get; set; } = 25d;
//        public double EDGE_VALUE_OUTER { get; set; } = 12.5d;
//        public double CENTER_VALUE_INNER { get; set; } = 35d;
//        public double CENTER_VALUE_OUTER { get; set; } = 17.5d;
//        public Algo(GameStatusDTO game, GameObject[][] grid, double future, double aggre, double myfoodvalue, double otherfood, double myflood, double otherflood, double voronoi, double edgeinner, double edgeouter, double centerinner, double centerouter)
//        {
//            IS_LOCAL = true;
//            _watch = Stopwatch.StartNew();
//            _rand = new();
//            _game = game;
//            _dir = Direction.LEFT;
//            var clone = DeepCloneGrid(grid);
//            _grid = clone;
//            _state = new State(clone);

//            FUTURE_UNCERTAINTY_FACOTR = future;
//            AGGRESSION_VALUE = aggre;
//            MY_FOOD_VALUE = myfoodvalue;
//            OTHER_FOOD_VALUE = otherfood;
//            MY_FLOODFILL_VALUE = myflood;
//            OTHER_FLOODFILL_VALUE = otherflood;
//            VORONOI_VALUE = voronoi;
//            EDGE_VALUE_INNER = edgeinner;
//            EDGE_VALUE_OUTER = edgeouter;
//            CENTER_VALUE_INNER = centerinner;
//            CENTER_VALUE_OUTER = centerouter;
//        }
//        */

//        public readonly GameStatusDTO Game;
//        private readonly Random _rand;
//        private GameObject[][] _grid;
//        private GameState _gameState = GameState.PLAYING;
//        private string MyId = Guid.NewGuid().ToString();

//        public Train(int height, int width, Snake me)
//        {
//            _rand = new Random();
//            Game = new GameStatusDTO
//            {
//                Board = new Board()
//                {
//                    Height = height,
//                    Width = width
//                },
//                Game = new Game()
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    Ruleset = new Ruleset()
//                    {
//                        Name = "New game",
//                        Settings = new RulesetSettings()
//                        {
//                            FoodSpawnChance = 100,
//                            HazardDamagePerTurn = 1,
//                            MinimumFood = 1
//                        },
//                        Version = "V1"
//                    }
//                },
//                Turn = 0
//            };

//            InitalizeGame(me);
//        }

//        public bool Play()
//        {
//            while (_gameState == GameState.PLAYING)
//            {
//                if (IsGameOver())
//                {
//                    _gameState = GameState.DONE;
//                    Snake me = Game.Board.Snakes.First(s => s.Id == MyId);
//                    Console.WriteLine("Total turns of this game: " + Game.Turn + " Did you win: " + me.IsAlive);
//                    if (me.IsAlive)
//                        return true;
//                    else
//                        return false;
//                }

//                AIMove();
//                SnakeCollsion();
//                FoodCollision();
//                Move();
//                Turn();

//                if (_gameState == GameState.REPLAY)
//                    Print();
//            }

//            return false;
//        }

//        private void AIMove()
//        {
//            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive))
//            {
//                Game.You = snake.Clone();
//                Algo alg = new(Game, _grid, snake.FUTURE_UNCERTAINTY_FACOTR, snake.AGGRESSION_VALUE, snake.MY_FOOD_VALUE, snake.OTHER_FOOD_VALUE, snake.MY_FLOODFILL_VALUE, snake.OTHER_FLOODFILL_VALUE, snake.VORONOI_VALUE, snake.EDGE_VALUE_INNER, snake.EDGE_VALUE_OUTER, snake.CENTER_VALUE_INNER, snake.CENTER_VALUE_OUTER);

//                Direction move = Direction.NO_MOVE;
//                if (snake.Id == MyId)
//                    move = alg.CalculateNextMove(snake, true);
//                else
//                    move = alg.CalculateNextMove(snake, false);

//                snake.Direction = move;
//            }
//        }

//        private bool IsInBounds(int x, int y)
//        {
//            return x >= 0 && x < Game.Board.Height && y >= 0 && y < Game.Board.Width;
//        }

//        private bool IsGameOver()
//        {
//            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive && s.Health <= 0 || s.IsAlive && !IsInBounds(s.Head.X, s.Head.Y)))
//            {
//                snake.IsAlive = false;
//                ClearFromGrid(snake);
//            }
//            return Game.Board.Snakes.Count(s => s.IsAlive) != 2;
//        }

//        private void Move()
//        {
//            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive))
//            {
//                int prevX = snake.Head.X, prevY = snake.Head.Y;
//                switch (snake.Direction)
//                {
//                    case Direction.UP:
//                        snake.Head.X -= 1;
//                        break;
//                    case Direction.DOWN:
//                        snake.Head.X += 1;
//                        break;
//                    case Direction.LEFT:
//                        snake.Head.Y -= 1;
//                        break;
//                    case Direction.RIGHT:
//                        snake.Head.Y += 1;
//                        break;
//                    default:
//                        break;
//                }

//                for (int i = 0; i < snake.Body.Count; i++)
//                {
//                    _grid[prevX][prevY] = GameObject.BODY;
//                    var temp = snake.Body[i];
//                    snake.Body[i] = new Point() { X = prevX, Y = prevY };
//                    prevX = temp.X;
//                    prevY = temp.Y;
//                }

//                if (IsInBounds(snake.Head.X, snake.Head.Y))
//                    _grid[snake.Head.X][snake.Head.Y] = GameObject.HEAD;
//                _grid[prevX][prevY] = GameObject.FLOOR;
//            }
//        }

//        private void SnakeCollsion()
//        {
//            var aliveSnakes = Game.Board.Snakes.Where(s => s.IsAlive);
//            foreach (var me in aliveSnakes)
//            {
//                foreach (var other in aliveSnakes.Where(s => s.IsAlive))
//                {
//                    if (me.Id != other.Id && me.Head.X == other.Head.X && me.Head.Y == other.Head.Y && other.Length >= me.Length)
//                    {
//                        other.SnakesEaten += 1;
//                        me.IsAlive = false;
//                        ClearFromGrid(me);
//                        break;
//                    }
//                    else if (other.Body.Skip(1).Any(b => other.IsAlive && b.X == me.Head.X && b.Y == me.Head.Y)) //Skip 1 to skip head -- Self and body collision
//                    {
//                        me.IsAlive = false;
//                        ClearFromGrid(me);
//                        break;
//                    }
//                }
//            }
//        }

//        private void ClearFromGrid(Snake snake)
//        {
//            //_grid[snake.Head.X][snake.Head.Y] = GameObject.FLOOR;
//            foreach (var point in snake.Body)
//            {
//                _grid[point.X][point.Y] = GameObject.FLOOR;
//            }
//        }

//        private void FoodCollision()
//        {
//            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive))
//            {
//                Point point = Game.Board.Food.FirstOrDefault(food => food.X == snake.Head.X && food.Y == snake.Head.Y);
//                if (point != null)
//                {
//                    _grid[point.X][point.Y] = GameObject.FLOOR;
//                    snake.Body.Add(new Point()
//                    {
//                        X = point.X,
//                        Y = point.Y
//                    });
//                    snake.Health = 100;
//                    snake.Length += 1;
//                    snake.Score += 1;
//                    Game.Game.Ruleset.Settings.FoodSpawnChance = 100;
//                    Game.Board.Food.Remove(point);
//                    GenerateFood();
//                }
//            }
//        }

//        private void Turn()
//        {
//            foreach (var snake in Game.Board.Snakes.Where(s => s.IsAlive))
//            {
//                snake.Health -= Game.Game.Ruleset.Settings.HazardDamagePerTurn;
//                snake.Moves += 1;
//            }
//            Game.Turn += 1;
//        }

//        private void Print()
//        {
//            Console.Clear();
//            for (int i = 0; i < _grid.Length + 2; i++)
//                Console.Write("#");

//            Console.WriteLine();
//            for (int i = 0; i < _grid.Length; i++)
//            {
//                Console.Write("#");
//                for (int j = 0; j < _grid[i].Length; j++)
//                    Console.Write((char)_grid[i][j]);
//                Console.Write("#");
//                Console.WriteLine();
//            }

//            for (int i = 0; i < _grid.Length + 2; i++)
//                Console.Write("#");

//            Thread.Sleep(Constants.FPS);
//        }

//        private void InitalizeGame(Snake me)
//        {
//            InitializeGrid();

//            Snake clone = GenerateSnake();
//            clone.AGGRESSION_VALUE = me.AGGRESSION_VALUE;
//            clone.CENTER_VALUE_INNER = me.CENTER_VALUE_INNER;
//            clone.CENTER_VALUE_OUTER = me.CENTER_VALUE_OUTER;
//            clone.EDGE_VALUE_INNER = me.EDGE_VALUE_INNER;
//            clone.EDGE_VALUE_OUTER = me.EDGE_VALUE_OUTER;
//            clone.FUTURE_UNCERTAINTY_FACOTR = me.FUTURE_UNCERTAINTY_FACOTR;
//            clone.MY_FLOODFILL_VALUE = me.MY_FLOODFILL_VALUE;
//            clone.MY_FOOD_VALUE = me.MY_FOOD_VALUE;
//            clone.OTHER_FLOODFILL_VALUE = me.OTHER_FLOODFILL_VALUE;
//            clone.OTHER_FOOD_VALUE = me.OTHER_FOOD_VALUE;
//            clone.VORONOI_VALUE = me.VORONOI_VALUE;
//            clone.Wins = me.Wins;
//            clone.Id = MyId;
//            Game.Board.Snakes.Add(clone);
//            _grid[clone.Head.X][clone.Head.Y] = GameObject.HEAD;

//            Snake other = GenerateSnake();
//            Game.Board.Snakes.Add(other);
//            _grid[other.Head.X][other.Head.Y] = GameObject.HEAD;

//            GenerateFood();
//        }

//        private void InitializeGrid()
//        {
//            _grid = new GameObject[Game.Board.Height][];
//            for (int i = 0; i < _grid.Length; i++)
//            {
//                _grid[i] = new GameObject[Game.Board.Width];
//                for (int j = 0; j < _grid[i].Length; j++)
//                    _grid[i][j] = GameObject.FLOOR;
//            }
//        }

//        private void GenerateFood()
//        {
//            int spawnChance = Game.Game.Ruleset.Settings.FoodSpawnChance;
//            if (spawnChance >= _rand.Next(0, spawnChance) + 1)
//            {
//                int x = _rand.Next(Game.Board.Height);
//                int y = _rand.Next(Game.Board.Width);
//                while (_grid[x][y] != GameObject.FLOOR)
//                {
//                    x = _rand.Next(Game.Board.Height);
//                    y = _rand.Next(Game.Board.Width);
//                }
//                _grid[x][y] = GameObject.FOOD;
//                Game.Board.Food.Add(new Point() { X = x, Y = y });
//                Game.Game.Ruleset.Settings.FoodSpawnChance = 15;
//            }
//        }

//        private Snake GenerateSnake()
//        {
//            List<(int x, int y)> startPos = new()
//            {
//                (1, 1),
//                (5, 1),
//                (9, 1),
//                (1, 5),
//                (9, 5),
//                (1, 9),
//                (5, 9),
//                (9, 9),
//            };
//            (int x, int y) head = startPos[_rand.Next(0, startPos.Count)];
//            while (_grid[head.x][head.y] != GameObject.FLOOR)
//            {
//                head = startPos[_rand.Next(0, startPos.Count)];
//            }
//            Snake snake = new()
//            {
//                Head = new Point()
//                {
//                    X = head.x,
//                    Y = head.y,
//                },
//                Health = 500,
//                Id = Guid.NewGuid().ToString(),
//                Length = 1,
//                IsAlive = true
//            };
//            snake.Body.Add(new Point() { X = head.x, Y = head.y });
//            snake.Body.Add(new Point() { X = head.x - 1, Y = head.y });
//            snake.Length = snake.Body.Count;
//            snake.Direction = (Direction)_rand.Next(0, 4);
//            return snake;
//        }
//    }
//}
