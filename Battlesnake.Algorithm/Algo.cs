using AlgoKit.Collections.Heaps;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using Battlesnake.Model;
using Battlesnake.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Battlesnake.Algorithm
{
    public class Algo
    {
        private readonly bool IS_LOCAL;
        private readonly GameStatusDTO _game;
        private readonly GameObject[][] _grid;
        private Direction _dir;
        private readonly Random _rand;
        private readonly Dictionary<(int x, int y), Direction> _dirs = new()
        {
            { (0, -1), Direction.LEFT },
            { (1, 0), Direction.DOWN },
            { (0, 1), Direction.RIGHT },
            { (-1, 0), Direction.UP },
        };
        private readonly List<Point> _diagonals = new()
        {
            new Point() { X = -1, Y = -1 }, //Upper left
            new Point() { X = -1, Y = 1 }, //Upper right
            new Point() { X = 1, Y = -1 }, //Lower left
            new Point() { X = 1, Y = 1 }, //Lower right
        };
        private readonly List<Point> _corners = new()
        {
            new() { X = 0, Y = 0 }, //Upper left
        };

        public Algo(GameStatusDTO game, Direction dir)
        {
            IS_LOCAL = false;
            _rand = new();
            UpdateCoordinates(game);
            _game = game;
            _dir = dir;
            _grid = GenerateGrid();
            int h = _grid.Length, w = _grid.First().Length;
            List<Point> missingCornerPieces = new()
            {
                new() { X = 0, Y = w - 1 }, //Upper right
                new() { X = h - 1, Y = 0 }, //Lower left
                new() { X = h - 1, Y = w - 1 }, //Lower right
            };
            _corners.AddRange(missingCornerPieces);
        }

        public Algo(GameStatusDTO game, GameObject[][] grid)
        {
            IS_LOCAL = true;
            _rand = new();
            _game = game;
            _dir = Direction.LEFT;
            _grid = grid;
            int h = _grid.Length, w = _grid.First().Length;
            List<Point> missingCornerPieces = new()
            {
                new() { X = 0, Y = w - 1 }, //Upper right
                new() { X = h - 1, Y = 0 }, //Lower left
                new() { X = h - 1, Y = w - 1 }, //Lower right
            };
            _corners.AddRange(missingCornerPieces);
        }

        public Direction CalculateNextMove(Snake me, bool allowMinimax)
        {
            Stopwatch watch = Stopwatch.StartNew();

            try
            {
                if (allowMinimax && _game.Board.Snakes.Count == 2)
                {
                    Snake other = _game.Board.Snakes.First(s => s.Id != me.Id);
                    Direction minimax = Minimax(me, other);
                    if (minimax != Direction.NO_MOVE)
                    {
                        _dir = minimax;
                        return _dir;
                    }
                }

                Point target = FindOptimalFood(me);
                if (target != null)
                {
                    Direction attack = Attack(me);
                    if (attack != Direction.NO_MOVE)
                    {
                        _dir = attack;
                        return _dir;
                    }

                    Direction bfs = BFS(me, target);
                    if (bfs != Direction.NO_MOVE)
                    {
                        _dir = bfs;
                        return _dir;
                    }
                }

                _dir = FloodFill(me);
            }
            catch (Exception) //Not possible to get an exception but used for finally 
            {
            }
            finally
            {
                if (Util.IsDebug) Console.WriteLine($"Run time took: {watch.Elapsed} -- next direction: {_dir}");
            }

            return _dir;
        }

        #region Attack function
        private Direction Attack(Snake me)
        {
            if (_game.Board.Snakes.Count == 1) return Direction.NO_MOVE;
            //if (Util.IsDebug) WriteDebugMessage("Searching for possible attack move");
            foreach (var point in _diagonals)
            {
                int dx = me.Head.X + point.X, dy = me.Head.Y + point.Y;
                if (IsInBounds(dx, dy) && _grid[dx][dy] == GameObject.HEAD)
                {
                    Snake other = _game.Board.Snakes.First(s => s.Head.X == dx && s.Head.Y == dy);
                    if (me.Length > other.Length)
                    {
                        Direction otherDirection = FloodFill(other);
                        if (Util.IsDebug) WriteDebugMessage($"Snake: {me.Id} attacks snake: {other.Id}");
                        switch ((point.X, point.Y))
                        {
                            case (-1, -1): //Upper left
                                if (otherDirection == Direction.DOWN && IsMoveableTile(me.Head.X, me.Head.Y - 1))
                                    return MoveLeft(me);
                                else if (otherDirection == Direction.RIGHT && IsMoveableTile(me.Head.X - 1, me.Head.Y))
                                    return MoveUp(me);
                                else
                                    break;
                            case (-1, 1): //Upper right
                                if (otherDirection == Direction.LEFT && IsMoveableTile(me.Head.X - 1, me.Head.Y))
                                    return MoveUp(me);
                                else if (otherDirection == Direction.DOWN && IsMoveableTile(me.Head.X, me.Head.Y + 1))
                                    return MoveRight(me);
                                else
                                    break;
                            case (1, -1): //Lower left
                                if (otherDirection == Direction.UP && IsMoveableTile(me.Head.X, me.Head.Y - 1))
                                    return MoveLeft(me);
                                else if (otherDirection == Direction.RIGHT && IsMoveableTile(me.Head.X + 1, me.Head.Y))
                                    return MoveDown(me);
                                else
                                    break;
                            case (1, 1): //Lower right
                                if (otherDirection == Direction.UP && IsMoveableTile(me.Head.X, me.Head.Y + 1))
                                    return MoveRight(me);
                                else if (otherDirection == Direction.LEFT && IsMoveableTile(me.Head.X + 1, me.Head.Y))
                                    return MoveDown(me);
                                else
                                    break;
                        }
                    }
                }
            }
            return Direction.NO_MOVE;
        }
        #endregion

        #region Mini max with alpha beta pruning
        private Direction Minimax(Snake me, Snake other)
        {
            GameObject[][] grid = CloneGrid(_grid);
            Snake meClone = me.Clone();
            Snake otherClone = other.Clone();
            //WriteDebugMessage("Board before minimax");
            //Print(grid);
            (double score, Direction move) = Minimax(grid, HeuristicConstants.MINIMAX_DEPTH);
            WriteDebugMessage("FINAL SCORE: " + score.ToString());
            //WriteDebugMessage(score.ToString() + " " + move);
            //WriteDebugMessage("Board after minimax");
            //Print(grid);
            if (move != Direction.NO_MOVE)
                return move;
            //Clear board to find new move
            int myIndex = _game.Board.Snakes.IndexOf(_game.Board.Snakes.First(s => s.Id == me.Id));
            int otherIndex = _game.Board.Snakes.IndexOf(_game.Board.Snakes.First(s => s.Id == other.Id));
            _game.Board.Snakes[myIndex] = meClone;
            _game.You = meClone;
            _game.Board.Snakes[otherIndex] = otherClone;
            return Direction.NO_MOVE;
        }

        private (double score, Direction move) Minimax(GameObject[][] grid, int depth, bool isMaximizingPlayer = true, int myFoodCount = 0, int otherFoodCount = 0, double alpha = double.MinValue, double beta = double.MaxValue)
        {
            Snake me = _game.You, other = _game.Board.Snakes.First(s => s.Id != _game.You.Id);
            if (depth == 0)
            {
                double evaluatedState = EvaluateState(grid, me, other, depth, myFoodCount, otherFoodCount);
                return (evaluatedState, Direction.NO_MOVE);
            }

            if (isMaximizingPlayer) //Only evaluate if game is over when it's my turn because it takes two depths for a turn
            {
                (double score, bool isGameOver) = EvaluateIfGameIsOver(me, other, depth);
                if (isGameOver) return (score, Direction.NO_MOVE);
            }

            List<(int x, int y)> moves = new()
            {
                (0, -1), //Left
                (1, 0), //Down
                (0, 1), //Right
                (-1, 0) //Up
            };
            //moves.Shuffle(); //Random shuffles the order of moves
            Direction bestMove = Direction.NO_MOVE;
            Snake currentSnake = isMaximizingPlayer ? me : other;
            double bestMoveScore = isMaximizingPlayer ? double.MinValue : double.MaxValue;
            int prevAppleCount = isMaximizingPlayer ? myFoodCount : otherFoodCount;
            for (int i = 0; i < moves.Count; i++) //For loop because it's faster in runtime
            {
                (int x, int y) = moves[i];
                int dx = x + currentSnake.Head.X, dy = y + currentSnake.Head.Y;
                if (IsMoveableTile(grid, dx, dy) || IsInBounds(dx, dy) && IsHeadCollision(me, other))
                //if (IsInBounds(dx, dy))
                //if (!isMaximizingPlayer && IsInBounds(dx, dy) || isMaximizingPlayer && IsMoveableTile(grid, dx, dy) || isMaximizingPlayer && IsInBounds(dx, dy) && IsHeadCollision(me, other))
                {
                    //var clone = CloneGrid(grid);
                    GameObject lastTile = grid[dx][dy];
                    Direction move = GetMove(x, y);
                    Point tail = new() { X = currentSnake.Body.Last().X, Y = currentSnake.Body.Last().Y };
                    int currentAppleCount = IsFoodTile(grid, dx, dy) ? prevAppleCount + 1 : prevAppleCount;
                    ShiftBodyForward(grid, currentSnake, x, y);
                    (double score, Direction move) eval = Minimax(grid, depth - 1, !isMaximizingPlayer, isMaximizingPlayer ? currentAppleCount : myFoodCount, !isMaximizingPlayer ? currentAppleCount : otherFoodCount, alpha, beta);
                    ShiftBodyBackwards(grid, lastTile, currentSnake, tail);
                    //WriteDebugMessage($"Best score: {bestMoveScore} current score: {eval.score}");
                    //if (!IsSame(grid, clone))
                    //{
                    //    Print(clone);
                    //    WriteDebugMessage("");
                    //    Print(grid);
                    //    Debug.Assert(false);
                    //}

                    if (isMaximizingPlayer)
                    {
                        if (eval.score > bestMoveScore)
                        {
                            bestMoveScore = eval.score;
                            bestMove = move;
                        }
                        alpha = Math.Max(alpha, eval.score);
                    }
                    else
                    {
                        if (eval.score < bestMoveScore)
                        {
                            bestMoveScore = eval.score;
                            bestMove = move;
                        }
                        beta = Math.Min(beta, eval.score);
                    }
                    if (beta <= alpha) break;
                }
            }
            return (bestMoveScore, bestMove);
        }

        private Direction GetMove(int x, int y)
        {
            if (x == 0 && y == -1)
                return Direction.LEFT;
            else if (x == 1 && y == 0)
                return Direction.DOWN;
            else if (x == 0 && y == 1)
                return Direction.RIGHT;
            else if (x == -1 && y == 0)
                return Direction.UP;
            else
                throw new Exception($"Invalid values: ({x}, {y})");
        }

        private void ShiftBodyForward(GameObject[][] grid, Snake snake, int x, int y)
        {
            //Clear snakes from board
            foreach (var s in _game.Board.Snakes)
                foreach (var b in s.Body)
                    grid[b.X][b.Y] = GameObject.FLOOR;
            //Move head + body of current snake forwards
            Point newHead = new() { X = snake.Body[0].X + x, Y = snake.Body[0].Y + y };
            snake.Body.Insert(0, new() { X = newHead.X, Y = newHead.Y });
            snake.Head = new() { X = newHead.X, Y = newHead.Y };
            snake.Body.RemoveAt(snake.Body.Count - 1);
            //Add snakes to board
            foreach (var s in _game.Board.Snakes)
            {
                foreach (var b in s.Body)
                    grid[b.X][b.Y] = GameObject.BODY;
                grid[s.Head.X][s.Head.Y] = GameObject.HEAD;
            }
        }

        private void ShiftBodyBackwards(GameObject[][] grid, GameObject lastTile, Snake snake, Point tail)
        {
            //Clear snakes from board
            foreach (var s in _game.Board.Snakes)
                foreach (var b in s.Body)
                    grid[b.X][b.Y] = GameObject.FLOOR;
            grid[snake.Head.X][snake.Head.Y] = lastTile; //Update correct tile from previous move
            //Move head + body of current snake backwards
            snake.Body.RemoveAt(0);
            Point newHead = new() { X = snake.Body[0].X, Y = snake.Body[0].Y };
            snake.Head = new() { X = newHead.X, Y = newHead.Y };
            snake.Body.Add(new() { X = tail.X, Y = tail.Y });
            //Add snakes to board
            foreach (var s in _game.Board.Snakes)
            {
                foreach (var b in s.Body)
                    grid[b.X][b.Y] = GameObject.BODY;
                grid[s.Head.X][s.Head.Y] = GameObject.HEAD;
            }
        }

        //https://github.com/calvinl4/battlesnake-winter-2022/blob/e28b8bb44077e148fc72042aa4d745b244d0b05a/utils/minimax.js#L126
        private double EvaluateState(GameObject[][] grid, Snake me, Snake other, int remainingDepth, int myFoodCount, int otherFoodCount)
        {
            int h = grid.Length, w = grid.First().Length;
            double score = 0d;
            Point myHead = me.Head;
            Point otherHead = other.Head;
            int myLength = me.Length;
            int otherLength = other.Length;
            int maxDistance = h + w;

            //Aggresion
            double aggresionScore = 0d;
            if (myLength >= otherLength + 2)
            {
                Point otherNeck = other.Body[1];
                Point otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y };
                (double distance, Point corner) corner = FindClosestCorner(otherHead);
                if (corner.distance == 0) //Other snake is in a corner
                {
                    if (corner.corner.X == 0 && corner.corner.Y == 0) //Upper left
                    {
                        if (otherNeck.X == 1) //Is moving upwards
                            otherSnakeMove = new() { X = 0, Y = 1 };
                        else //Is moving left
                            otherSnakeMove = new() { X = 1, Y = 0 };
                    }
                    else if (corner.corner.X == h - 1 && corner.corner.Y == 0) //Lower left
                    {
                        if (otherNeck.X == 1) //Is moving upwards
                            otherSnakeMove = new() { X = 0, Y = w - 2 };
                        else //Is moving right
                            otherSnakeMove = new() { X = 1, Y = w - 1 };
                    }
                    else if (corner.corner.X == 0 && corner.corner.Y == w - 1) //Upper right
                    {
                        if (otherNeck.X == 1) //Is moving downwards
                            otherSnakeMove = new() { X = h - 1, Y = 1 };
                        else //Is moving left
                            otherSnakeMove = new() { X = h - 2, Y = 0 };
                    }
                    else if (corner.corner.X == h - 1 && corner.corner.Y == w - 1) //Lower right
                    {
                        if (otherNeck.X == 1) //Is moving downwards
                            otherSnakeMove = new() { X = h - 1, Y = w - 2 };
                        else //Is moving right
                            otherSnakeMove = new() { X = h - 2, Y = w - 1 };
                    }
                    else
                        throw new Exception("Invalid corner");
                }
                else if (otherHead.X == 0 || other.Head.X == h - 1 || other.Head.Y == 0 || other.Head.Y == w - 1) //Other snake is moving on a edge line
                {
                    if (otherHead.Y == 0 && otherNeck.Y == 1 || otherHead.Y == w - 1 && otherNeck.X == w - 2)
                    {
                        Point possibleMove1 = new() { X = otherHead.X + 1, Y = otherHead.Y };
                        Point possibleMove2 = new() { X = otherHead.X - 1, Y = otherHead.Y };
                        Point closestFoodMove1 = ChooseClosestFood(possibleMove1);
                        int distanceMove1 = ManhattenDistance(possibleMove1.X, possibleMove1.Y, closestFoodMove1.X, closestFoodMove1.Y);
                        Point closestFoodMove2 = ChooseClosestFood(possibleMove2);
                        int distanceMove2 = ManhattenDistance(possibleMove2.X, possibleMove2.Y, closestFoodMove2.X, closestFoodMove2.Y);
                        otherSnakeMove = distanceMove1 <= distanceMove2 ? possibleMove1 : possibleMove2;
                    }
                    else if (otherHead.X == 0 && otherNeck.X == 1 || otherHead.X == h - 1 && otherNeck.X == h - 2)
                    {
                        Point possibleMove1 = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                        Point possibleMove2 = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                        Point closestFoodMove1 = ChooseClosestFood(possibleMove1);
                        int distanceMove1 = ManhattenDistance(possibleMove1.X, possibleMove1.Y, closestFoodMove1.X, closestFoodMove1.Y);
                        Point closestFoodMove2 = ChooseClosestFood(possibleMove2);
                        int distanceMove2 = ManhattenDistance(possibleMove2.X, possibleMove2.Y, closestFoodMove2.X, closestFoodMove2.Y);
                        otherSnakeMove = distanceMove1 <= distanceMove2 ? possibleMove1 : possibleMove2;
                    }
                }
                else //Try to predict snake move
                {
                    if (otherHead.X - 1 == otherNeck.X) //Going upwards
                        otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                    else if (otherHead.X + 1 == otherNeck.X) //Going downwards
                        otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                    else if (otherHead.Y - 1 == otherNeck.Y) //Going right
                        otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                    else if (otherHead.Y + 1 == otherNeck.Y) //Going left
                        otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                }

                if (!IsInBounds(otherSnakeMove.X, otherSnakeMove.Y)) //Not a valid move
                {
                    List<Point> safeTiles = SafeTiles(grid, other);
                    int index = _rand.Next(0, safeTiles.Count);
                    otherSnakeMove = safeTiles[index];
                }

                int distanceToOtherSnake = ManhattenDistance(myHead.X, myHead.Y, otherSnakeMove.X, otherSnakeMove.Y);
                aggresionScore = Math.Abs(maxDistance - distanceToOtherSnake) * HeuristicConstants.AGGRESSION_VALUE;
            }
            score += aggresionScore;

            //Is game over
            (double score, bool isGameOver) evaluateIfGameIsOver = EvaluateIfGameIsOver(me, other, remainingDepth);
            if (evaluateIfGameIsOver.isGameOver)
            {
                score = evaluateIfGameIsOver.score;
                return score;
            }

            //Food            
            double foodScore = 0d;
            if (me.Health <= 40 || myLength < otherLength + 2) //I am hungry or they are 2 sizes larger than me
            {
                if (myFoodCount > 0)
                    foodScore = HeuristicConstants.MY_FOOD_VALUE * myFoodCount;
                else
                {
                    Point myClosestFood = FindClosestFood(me);
                    int distanceToMyClosestFood = ManhattenDistance(myHead.X, myHead.Y, myClosestFood.X, myClosestFood.Y);
                    foodScore = Math.Pow((maxDistance - distanceToMyClosestFood) / 4, 2);
                }
            }
            score += foodScore;

            double theirFoodScore = 0d;
            if (other.Health <= 40 || otherLength < myLength + 2) //They are hungry or I am two sizes larger
            {
                if (otherFoodCount > 0)
                    theirFoodScore -= HeuristicConstants.OTHER_FOOD_VALUE * otherFoodCount;
                else
                {
                    Point otherClosestFood = FindClosestFood(other);
                    int distanceToTheirClosestFood = ManhattenDistance(otherHead.X, otherHead.Y, otherClosestFood.X, otherClosestFood.Y);
                    theirFoodScore = Math.Pow((maxDistance - distanceToTheirClosestFood) / 4, 2);
                }
            }
            score += theirFoodScore;

            //Flood fill
            double myFloodFillScore = CalculateFloodfillScore(grid, me) * HeuristicConstants.MY_FLOODFILL_VALUE;
            double otherFloodFillScore = -1d * CalculateFloodfillScore(grid, other) * HeuristicConstants.OTHER_FLOODFILL_VALUE;
            double floodFillScore = myFloodFillScore + otherFloodFillScore;
            score += floodFillScore;

            //Edge
            double edgeScore = 0d;
            double outerBound = HeuristicConstants.EDGE_VALUE_INNER;
            double secondOuterBound = HeuristicConstants.EDGE_VALUE_OUTER;
            //Me -- Bad for being close to the edge
            if (myHead.X == 0 || myHead.X == h - 1 || myHead.Y == 0 || myHead.Y == w - 1)
                edgeScore -= outerBound / 2;
            else if (myHead.X == 1 || myHead.X == h - 2 || myHead.Y == 1 || myHead.Y == w - 2)
                edgeScore -= secondOuterBound / 2;
            //Other -- Good for me if other is close to the edge
            if (otherHead.X == 0 || otherHead.X == h - 1 || otherHead.Y == 0 || otherHead.Y == w - 1)
                edgeScore += outerBound / 2;
            else if (otherHead.X == 1 || otherHead.X == h - 2 || otherHead.Y == 1 || otherHead.Y == w - 2)
                edgeScore += secondOuterBound / 2;
            score += edgeScore;

            if (score >= 57289761)
            {
                Console.WriteLine();
            }

            return score;
        }

        private (double distance, Point corner) FindClosestCorner(Point head)
        {
            int h = _grid.Length - 1, w = _grid.First().Length - 1;
            double shortestDistance = int.MaxValue;
            Point closestCorner = new() { X = 0, Y = 0 };
            //Check upper left
            double currentDistance = Math.Pow(head.X, 2) + Math.Pow(head.Y, 2);
            if (shortestDistance > currentDistance)
            {
                shortestDistance = currentDistance;
                closestCorner = new() { X = 0, Y = 0 };
            }
            //Check upper right
            currentDistance = Math.Pow(head.X, 2) + Math.Pow(head.Y - w, 2);
            if (shortestDistance > currentDistance)
            {
                shortestDistance = currentDistance;
                closestCorner = new() { X = 0, Y = w };
            }
            //Check lower left
            currentDistance = Math.Pow(head.X - h, 2) + Math.Pow(head.Y, 2);
            if (shortestDistance > currentDistance)
            {
                shortestDistance = currentDistance;
                closestCorner = new() { X = h, Y = 0 };
            }
            //Check lower right
            currentDistance = Math.Pow(head.X - h, 2) + Math.Pow(head.Y - w, 2);
            if (shortestDistance > currentDistance)
            {
                shortestDistance = currentDistance;
                closestCorner = new() { X = h, Y = w };
            }
            return (shortestDistance, closestCorner);
        }

        private double CalculateFloodfillScore(GameObject[][] grid, Snake me)
        {
            int cavernSize = FloodFillWithLimit(grid, me, me.Length);
            if (cavernSize >= HeuristicConstants.SAFE_CAVERN_SIZE * me.Length) return 0;
            double floodFillScore = (HeuristicConstants.FLOODFILL_MAX - HeuristicConstants.FLOODFILL_MIN) / Math.Sqrt(HeuristicConstants.SAFE_CAVERN_SIZE * HeuristicConstants.LARGEST_SNAKE) * Math.Sqrt(cavernSize) - HeuristicConstants.FLOODFILL_MAX;
            return floodFillScore;
        }

        private List<Point> SafeTiles(GameObject[][] grid, Snake me)
        {
            List<Point> neighbours = new();
            foreach (var (x, y) in _dirs.Keys)
            {
                int dx = x + me.Head.X, dy = y + me.Head.Y;
                if (IsMoveableTile(grid, dx, dy)) neighbours.Add(new Point() { X = dx, Y = dy });
            }
            return neighbours;
        }

        private (double score, bool isGameOver) EvaluateIfGameIsOver(Snake me, Snake other, int remainingDepth)
        {
            Point myHead = me.Head;
            Point otherHead = other.Head;
            bool mySnakeDead = false;
            bool mySnakeMaybeDead = false;
            bool otherSnakeDead = false;
            bool otherSnakeMaybeDead = false;
            bool headOnCollsion = false;

            if (!IsInBounds(myHead.X, myHead.Y))
                mySnakeDead = true;

            if (!IsInBounds(otherHead.X, otherHead.Y))
                otherSnakeDead = true;

            if (IsHeadCollision(me, other))
            {
                headOnCollsion = true;
                if (me.Length == other.Length)
                {
                    mySnakeMaybeDead = true;
                    otherSnakeMaybeDead = true;
                }
                else if (me.Length > other.Length)
                    otherSnakeDead = true;
                else
                    mySnakeMaybeDead = true;
            }

            int mySnakeHeadOnCount = 0, otherSnakeHeadOnCount = 0;
            foreach (var s in _game.Board.Snakes)
            {
                foreach (var b in s.Body)
                {
                    if (b.X == myHead.X && b.Y == myHead.Y)
                        mySnakeHeadOnCount++;
                    if (b.X == otherHead.X && b.Y == otherHead.Y)
                        otherSnakeHeadOnCount++;
                }
            }

            if (!headOnCollsion) //Maybe body collsion
            {
                if (mySnakeHeadOnCount > 1)
                    mySnakeDead = true;

                if (otherSnakeHeadOnCount > 1)
                    otherSnakeDead = true;
            }
            else
            {
                if (mySnakeHeadOnCount > 2 && otherSnakeHeadOnCount > 2)
                {
                    mySnakeDead = true;
                    otherSnakeDead = true;
                }
            }

            double score;
            if (mySnakeDead)
                score = AdjustForFutureUncetainty(-1000, remainingDepth);
            else if (mySnakeMaybeDead)
                score = AdjustForFutureUncetainty(-500, remainingDepth);
            else if (otherSnakeMaybeDead)
                score = AdjustForFutureUncetainty(500, remainingDepth);
            else if (otherSnakeDead)
                score = AdjustForFutureUncetainty(1000, remainingDepth);
            else
                score = AdjustForFutureUncetainty(0, remainingDepth);

            bool isGameOver = false;
            if (mySnakeDead || otherSnakeDead || mySnakeMaybeDead || otherSnakeMaybeDead) isGameOver = true;

            return (score, isGameOver);
        }

        private double AdjustForFutureUncetainty(double score, int remainingDepth)
        {
            int pow = HeuristicConstants.MINIMAX_DEPTH - remainingDepth - 2;
            double futureUncertainty = Math.Pow(HeuristicConstants.FUTURE_UNCERTAINTY_FACOTR, pow);
            return score * futureUncertainty;
        }
        #endregion

        #region Flood fill
        private Direction FloodFill(Snake me)
        {
            //if (Util.IsDebug) WriteDebugMessage("Using floodfill");
            List<(int x, int y, int freeTiles)> list = new();
            foreach (var (x, y) in _dirs.Keys)
            {
                int dx = me.Head.X + x;
                int dy = me.Head.Y + y;
                if (IsMoveableTile(dx, dy))
                {
                    var clone = CloneGrid(_grid);
                    var freeTiles = FloodFill(clone, dx, dy);
                    dx -= me.Head.X;
                    dy -= me.Head.Y;
                    list.Add((dx, dy, freeTiles));
                }
            }

            if (list.Count > 0)
            {
                (int x, int y, int freeTiles) = list.OrderByDescending(v => v.freeTiles).First();
                return _dirs[(x, y)];
            }

            return _dir;
        }

        private int FloodFillWithLimit(GameObject[][] grid, Snake me, int limit)
        {
            Queue<(int x, int y)> queue = new();
            HashSet<(int x, int y)> isVisited = new();
            queue.Enqueue((me.Head.X, me.Head.Y));
            isVisited.Add((me.Head.X, me.Head.Y));
            int count = 1; //One because we added our head
            while (queue.Any())
            {
                (int x, int y) current = queue.Dequeue();
                if (count >= limit) return count;
                foreach (var (x, y) in _dirs.Keys)
                {
                    int dx = x + current.x, dy = y + current.y;
                    if (IsMoveableTile(grid, dx, dy) && isVisited.Add((dx, dy)))
                    {
                        queue.Enqueue((dx, dy));
                        count++;
                    }
                }
            }
            return count;
        }

        private int FloodFill(GameObject[][] grid, int x, int y)
        {
            if (!IsMoveableTile(grid, x, y)) return 0;
            int sum = 0;
            grid[x][y] = GameObject.BODY; //Set visited
            foreach ((int x, int y) item in _dirs.Keys)
            {
                int dx = x + item.x;
                int dy = y + item.y;
                sum += 1 + FloodFill(grid, dx, dy);
            }
            return sum;
        }
        #endregion

        #region BFS
        private Direction BFS(Snake me, Point target)
        {
            //if (Util.IsDebug) WriteDebugMessage("Searching for possible BFS move");
            Comparer<int> comparer = Comparer<int>.Default;
            PairingHeap<int, (int x, int y, List<(int x, int y)> steps)> queue = new(comparer);
            HashSet<(int x, int y)> isVisited = new();
            foreach (var (x, y) in _dirs.Keys)
            {
                int dx = me.Head.X + x, dy = me.Head.Y + y;
                if (IsMoveableTile(dx, dy))
                {
                    int distance = ManhattenDistance(target.X, target.Y, dx, dy);
                    queue.Add(distance, (dx, dy, new List<(int x, int y)>() { (dx, dy) }));
                    isVisited.Add((dx, dy));
                }
            }

            while (!queue.IsEmpty)
            {
                var current = queue.Pop().Value;

                if (current.x == target.X && current.y == target.Y)
                {
                    (int x, int y) = current.steps.First();
                    int dx = x - me.Head.X, dy = y - me.Head.Y;
                    return _dirs[(dx, dy)];
                }

                foreach (var (x, y) in _dirs.Keys)
                {
                    int dx = current.x + x, dy = current.y + y;
                    if (IsMoveableTile(dx, dy) && isVisited.Add((dx, dy)))
                    {
                        int distance = ManhattenDistance(target.X, target.Y, dx, dy);
                        queue.Add(distance, (dx, dy, new List<(int x, int y)>(current.steps) { (dx, dy) }));
                    }
                }
            }
            return Direction.NO_MOVE;
        }
        #endregion

        #region Choose food
        private Point FindOptimalFood(Snake me)
        {
            //if (Util.IsDebug) WriteDebugMessage("Searching for best food");
            Dictionary<string, List<(Point food, int distance)>> distances = new();
            foreach (var snake in _game.Board.Snakes)
                distances.Add(snake.Id, new List<(Point food, int distance)>());

            foreach (var food in _game.Board.Food)
            {
                foreach (var snake in _game.Board.Snakes)
                {
                    int distance = ManhattenDistance(food.X, food.Y, snake.Head.X, snake.Head.Y);
                    distances[snake.Id].Add((food, distance));
                }
            }

            //if (Util.IsDebug) WriteDebugMessage("Food on board: " + _game.Board.Food.Count);
            if (distances[me.Id].Count == 0) //No food found
                return null;

            foreach (var snake in distances)
                distances[snake.Key] = snake.Value.OrderBy(food => food.distance).ToList(); //Sort distances by shortest distance

            List<(Point food, int distance)> myFoodDistances = distances[me.Id];
            if (Util.IsDebug) WriteDebugMessage(string.Join(", ", myFoodDistances));
            if (distances.Count == 1 || myFoodDistances.Count == 1)
                return myFoodDistances.First().food;

            if (distances.Any(kv => kv.Key != me.Id && kv.Value.First().distance == myFoodDistances.First().distance && kv.Value.First().food == myFoodDistances.First().food)) //Check if other snakes has same shortest distance to the same food as me
            {
                if (_game.Board.Snakes.All(s => s.Id != me.Id && me.Length > s.Length)) //Check if I'm the biggest snake on the board if so target the food
                    return myFoodDistances.First().food;
                else if (_game.Board.Food.Count == 2) //Assume they will go for the closest food because they are bigger or same size as me
                    return myFoodDistances.Last().food;
                Dictionary<string, List<(Point food, int distance)>> otherSnakeFoodDistances = distances.Where(kv => kv.Key != me.Id && kv.Value.First().distance == myFoodDistances.First().distance && kv.Value.First().food == myFoodDistances.First().food).ToDictionary(k => k.Key, v => v.Value);
                int n = otherSnakeFoodDistances.Values.First().Count; //Length of the list
                for (int i = 1; i < n; i++)
                {
                    (Point food, int distance) myCurrent = myFoodDistances[i];
                    bool isMatch = true;
                    foreach (var kv in otherSnakeFoodDistances)
                    {
                        int otherDistance = kv.Value.First(f => f.food == myCurrent.food).distance;
                        if (myCurrent.distance > otherDistance)
                        {
                            isMatch = false;
                            break;
                        }
                    }

                    if (isMatch)
                    {
                        //if (Util.IsDebug) WriteDebugMessage($"My taget -- Distance: {myCurrent.distance} ({myCurrent.food.X} {myCurrent.food.Y})");
                        return myCurrent.food;
                    }
                }
            }

            return myFoodDistances.First().food;
        }

        private Point ChooseClosestFood(Point position)
        {
            int minDistance = _game.Board.Food.Min(f => ManhattenDistance(f.X, f.Y, position.X, position.Y));
            return _game.Board.Food.First(f => ManhattenDistance(f.X, f.Y, position.X, position.Y) == minDistance);
        }

        private Point FindClosestFood(Snake me)
        {
            int minDistance = _game.Board.Food.Min(f => ManhattenDistance(f.X, f.Y, me.Head.X, me.Head.Y));
            return _game.Board.Food.First(f => ManhattenDistance(f.X, f.Y, me.Head.X, me.Head.Y) == minDistance);
        }
        #endregion

        #region Helper functions
        private void WriteDebugMessage(string message)
        {
            if (IS_LOCAL)
                Console.WriteLine(message);
            else
                Debug.WriteLine(message);
        }

        private bool IsMoveableTile(int x, int y)
        {
            return IsFreeTile(x, y) || IsFoodTile(x, y);
        }

        private bool IsFoodTile(int x, int y)
        {
            return IsInBounds(x, y) && _grid[x][y] == GameObject.FOOD;
        }

        private bool IsFreeTile(int x, int y)
        {
            return IsInBounds(x, y) && _grid[x][y] == GameObject.FLOOR;
        }

        private bool IsMoveableTile(GameObject[][] grid, int x, int y)
        {
            return IsFreeTile(grid, x, y) || IsFoodTile(grid, x, y);
        }

        private bool IsFoodTile(GameObject[][] grid, int x, int y)
        {
            return IsInBounds(x, y) && grid[x][y] == GameObject.FOOD;
        }

        private bool IsFreeTile(GameObject[][] grid, int x, int y)
        {
            return IsInBounds(x, y) && grid[x][y] == GameObject.FLOOR;
        }

        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _game.Board.Height && y >= 0 && y < _game.Board.Width;
        }

        private bool IsHeadCollision(Snake me, Snake other)
        {
            return me.Head.X == other.Head.X && me.Head.Y == other.Head.Y;
        }

        private GameObject[][] CloneGrid(GameObject[][] grid)
        {
            int h = _game.Board.Height;
            int w = _game.Board.Width;
            GameObject[][] clone = new GameObject[h][];
            for (int i = 0; i < h; i++)
            {
                clone[i] = new GameObject[w];
                for (int j = 0; j < w; j++)
                {
                    clone[i][j] = grid[i][j];
                }
            }
            return clone;
        }

        private GameObject[][] GenerateGrid()
        {
            GameObject[][] grid = new GameObject[_game.Board.Height][];
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = new GameObject[_game.Board.Width];
                for (int j = 0; j < grid[i].Length; j++)
                {
                    if (_game.Board.Food.Any(f => f.X == i && f.Y == j))
                        grid[i][j] = GameObject.FOOD;
                    else if (_game.Board.Snakes.Any(s => s.Head.X == i && s.Head.Y == j))
                        grid[i][j] = GameObject.HEAD;
                    else if (_game.Board.Snakes.Any(s => s.Body.Any(b => b.X == i && b.Y == j)))
                        grid[i][j] = GameObject.BODY;
                    else
                        grid[i][j] = GameObject.FLOOR;
                }
            }
            return grid;
        }

        private int ManhattenDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
        }

        public void Print(GameObject[][] grid = null)
        {
            if (grid == null) grid = _grid;
            if (IS_LOCAL)
            {
                //Console.WriteLine();
                for (int i = 0; i < grid.Length + 2; i++)
                    Console.Write("#");

                Console.WriteLine();
                for (int i = 0; i < grid.Length; i++)
                {
                    Console.Write("#");
                    for (int j = 0; j < grid[i].Length; j++)
                        Console.Write((char)grid[i][j]);
                    Console.Write("#");
                    Console.WriteLine();
                }

                for (int i = 0; i < grid.Length + 2; i++)
                    Console.Write("#");
                Console.WriteLine();
                Thread.Sleep(25);
            }
            else
            {
                Debug.WriteLine("");
                for (int i = 0; i < grid.Length + 2; i++)
                    Debug.Write("#");

                Debug.WriteLine("");
                for (int i = 0; i < grid.Length; i++)
                {
                    Debug.Write("#");
                    for (int j = 0; j < grid[i].Length; j++)
                        Debug.Write((char)grid[i][j]);
                    Debug.Write("#");
                    Debug.WriteLine("");
                }

                for (int i = 0; i < grid.Length + 2; i++)
                    Debug.Write("#");
                Debug.WriteLine("");
            }
        }

        private void UpdateCoordinates(GameStatusDTO game)
        {
            int h = game.Board.Height - 1;
            foreach (var item in game.Board.Food)
            {
                item.Y = h - item.Y;
                var temp = item.X;
                item.X = item.Y;
                item.Y = temp;
            }

            foreach (var s in game.Board.Snakes)
            {
                s.Head.Y = h - s.Head.Y;
                int temp = s.Head.X;
                s.Head.X = s.Head.Y;
                s.Head.Y = temp;
                foreach (var b in s.Body)
                {
                    b.Y = h - b.Y;
                    temp = b.X;
                    b.X = b.Y;
                    b.Y = temp;
                }
            }

            game.You.Head.Y = h - game.You.Head.Y;
            int t = game.You.Head.X;
            game.You.Head.X = game.You.Head.Y;
            game.You.Head.Y = t;
            foreach (var b in game.You.Body)
            {
                b.Y = h - b.Y;
                var temp = b.X;
                b.X = b.Y;
                b.Y = temp;
            }
        }

        private bool IsSame(GameObject[][] grid, GameObject[][] other)
        {
            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid[i].Length; j++)
                {
                    if (grid[i][j] != other[i][j])
                        return false;
                }
            }
            return true;
        }

        private Direction MoveLeft(Snake snake)
        {
            if (snake.Direction != Direction.RIGHT)
                return Direction.LEFT;
            else
                return Direction.NO_MOVE;
        }

        private Direction MoveRight(Snake snake)
        {
            if (snake.Direction != Direction.LEFT)
                return Direction.RIGHT;
            else
                return Direction.NO_MOVE;
        }

        private Direction MoveUp(Snake snake)
        {
            if (snake.Direction != Direction.DOWN)
                return Direction.UP;
            else
                return Direction.NO_MOVE;
        }

        private Direction MoveDown(Snake snake)
        {
            if (snake.Direction != Direction.UP)
                return Direction.DOWN;
            else
                return Direction.NO_MOVE;
        }
        #endregion
    }
}
