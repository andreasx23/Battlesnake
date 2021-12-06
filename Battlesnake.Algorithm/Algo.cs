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
        private readonly Stopwatch _watch;
        private bool IsTimeoutThresholdReached => !Util.IsDebug && _watch.Elapsed >= TimeSpan.FromMilliseconds(_game.Game.Timeout - 25);

        public Algo(GameStatusDTO game, Direction dir, Stopwatch watch)
        {
            IS_LOCAL = false;
            _rand = new();
            _watch = watch;
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
            _watch = Stopwatch.StartNew();
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
                if (Util.IsDebug) WriteDebugMessage($"Run time took: {_watch.Elapsed} -- next direction: {_dir}");
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
            GameObject[][] gridClone = CloneGrid(_grid);
            Snake meClone = me.Clone();
            Snake otherClone = other.Clone();
            (double score, Direction move) = Minimax(gridClone, meClone, otherClone, HeuristicConstants.MINIMAX_DEPTH);
            WriteDebugMessage($"Best score from minimax: {score} -- move to perform: {move}");
            return move;
        }

        private (double score, Direction move) Minimax(GameObject[][] grid, Snake me, Snake other, int depth, bool isMaximizingPlayer = true, int myFoodCount = 0, int otherFoodCount = 0, double alpha = double.MinValue, double beta = double.MaxValue)
        {
            if (IsTimeoutThresholdReached) return (isMaximizingPlayer ? double.MinValue : double.MaxValue, Direction.NO_MOVE); //Failsafe to handle timeout

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
            Direction bestMove = Direction.NO_MOVE;
            List<Snake> snakes = new() { me, other };
            Snake currentSnake = isMaximizingPlayer ? me : other;
            double bestMoveScore = isMaximizingPlayer ? double.MinValue : double.MaxValue;
            int prevAppleCount = isMaximizingPlayer ? myFoodCount : otherFoodCount;
            for (int i = 0; i < moves.Count; i++) //For loop because it's faster in runtime
            {
                (int x, int y) = moves[i];
                int dx = x + currentSnake.Head.X, dy = y + currentSnake.Head.Y;
                if (IsInBounds(dx, dy))
                {
                    //Change state of the game
                    Point tail = new() { X = currentSnake.Body.Last().X, Y = currentSnake.Body.Last().Y };
                    Direction move = GetMove(x, y);
                    GameObject lastTile = grid[dx][dy];
                    int currentHp = currentSnake.Health;
                    int currentLength = currentSnake.Length;
                    bool isFoodTile = IsFoodTile(grid, dx, dy);
                    int currentAppleCount = isFoodTile ? prevAppleCount + 1 : prevAppleCount;
                    currentSnake.Health = isFoodTile ? HeuristicConstants.MAX_HEALTH : currentHp - _game.Game.Ruleset.Settings.HazardDamagePerTurn;
                    currentSnake.Length = isFoodTile ? currentLength + 1 : currentLength;
                    //Move the snake
                    ClearSnakesFromGrid(grid, snakes);
                    ShiftBodyForward(grid, currentSnake, x, y, isFoodTile);
                    ApplySnakesToGrid(grid, snakes);
                    (double score, Direction move) eval = Minimax(grid: grid,
                                                                    me: isMaximizingPlayer ? currentSnake : me,
                                                                    other: !isMaximizingPlayer ? currentSnake : other,
                                                                    depth: depth - 1,
                                                                    isMaximizingPlayer: !isMaximizingPlayer,
                                                                    myFoodCount: isMaximizingPlayer ? currentAppleCount : myFoodCount,
                                                                    otherFoodCount: !isMaximizingPlayer ? currentAppleCount : otherFoodCount,
                                                                    alpha: alpha,
                                                                    beta: beta);
                    //Move the snake back
                    ClearSnakesFromGrid(grid, snakes);
                    ShiftBodyBackwards(grid, lastTile, currentSnake, tail, isFoodTile);
                    ApplySnakesToGrid(grid, snakes);
                    //Revert changes made doing previous state
                    currentSnake.Health = currentHp;
                    currentSnake.Length = currentLength;
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

        private void ClearSnakesFromGrid(GameObject[][] grid, List<Snake> snakes)
        {
            //Clear snakes from board
            foreach (var s in snakes)
                foreach (var b in s.Body)
                    grid[b.X][b.Y] = GameObject.FLOOR;
        }

        private void ApplySnakesToGrid(GameObject[][] grid, List<Snake> snakes)
        {
            //Add snakes to board
            foreach (var s in snakes)
            {
                foreach (var b in s.Body)
                    grid[b.X][b.Y] = GameObject.BODY;
                grid[s.Head.X][s.Head.Y] = GameObject.HEAD;
            }
        }

        private void ShiftBodyForward(GameObject[][] grid, Snake snake, int x, int y, bool isFoodTile)
        {
            //Move head + body of current snake forwards
            Point newHead = new() { X = snake.Body[0].X + x, Y = snake.Body[0].Y + y };
            snake.Body.Insert(0, new() { X = newHead.X, Y = newHead.Y });
            snake.Head = new() { X = newHead.X, Y = newHead.Y };
            if (!isFoodTile) snake.Body.RemoveAt(snake.Body.Count - 1);
        }

        private void ShiftBodyBackwards(GameObject[][] grid, GameObject lastTile, Snake snake, Point tail, bool isFoodTile)
        {
            grid[snake.Head.X][snake.Head.Y] = lastTile; //Update correct tile from previous move
            //Move head + body of current snake backwards
            snake.Body.RemoveAt(0);
            Point newHead = new() { X = snake.Body[0].X, Y = snake.Body[0].Y };
            snake.Head = new() { X = newHead.X, Y = newHead.Y };
            if (isFoodTile) snake.Body.RemoveAt(snake.Body.Count - 1);
            snake.Body.Add(new() { X = tail.X, Y = tail.Y });
        }

        private List<Point> GetFoodFromGrid(GameObject[][] grid)
        {
            int h = grid.Length, w = grid.First().Length;
            List<Point> foods = new();
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                    if (IsFoodTile(grid, i, j))
                        foods.Add(new Point() { X = i, Y = j });
            return foods;
        }

        private double EvaluateState(GameObject[][] grid, Snake me, Snake other, int remainingDepth, int myFoodCount, int otherFoodCount)
        {
            int h = grid.Length, w = grid.First().Length;
            double score = 0d;
            Point myHead = me.Head;
            Point otherHead = other.Head;
            int myLength = me.Length;
            int otherLength = other.Length;
            int maxDistance = h + w;
            List<Point> availableFoods = GetFoodFromGrid(grid);

            //Aggresion
            double aggresionScore = 0d;
            if (myLength >= otherLength + 2)
            {
                Point otherNeck = other.Body[1];
                Point otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y };
                (double distance, Point corner) closestCornor = FindClosestCorner(otherHead);
                bool isCornorChasing = false;
                if (closestCornor.distance == 0) //Other snake is in a corner
                {
                    if (closestCornor.corner.X == 0 && closestCornor.corner.Y == 0) //Upper left
                    {
                        if (otherNeck.X == 1) //Next move is right
                            otherSnakeMove = new() { X = 0, Y = 1 };
                        else //Next move is down
                            otherSnakeMove = new() { X = 1, Y = 0 };
                    }
                    else if (closestCornor.corner.X == h - 1 && closestCornor.corner.Y == 0) //Lower left
                    {
                        if (otherNeck.X == h - 2) //Next move is right
                            otherSnakeMove = new() { X = h - 1, Y = 1 };
                        else //Next move is up
                            otherSnakeMove = new() { X = h - 2, Y = 0 };
                    }
                    else if (closestCornor.corner.X == 0 && closestCornor.corner.Y == w - 1) //Upper right
                    {
                        if (otherNeck.X == 1) //Next move is left
                            otherSnakeMove = new() { X = 0, Y = w - 2 };
                        else //Next move is down
                            otherSnakeMove = new() { X = 1, Y = w - 1 };
                    }
                    else if (closestCornor.corner.X == h - 1 && closestCornor.corner.Y == w - 1) //Lower right
                    {
                        if (otherNeck.X == h - 2) //Next move is left
                            otherSnakeMove = new() { X = h - 1, Y = w - 2 };
                        else //Next move is up
                            otherSnakeMove = new() { X = h - 2, Y = w - 1 };
                    }
                }
                else if (otherHead.X == 0 || other.Head.X == h - 1 || other.Head.Y == 0 || other.Head.Y == w - 1) //Other snake is moving on a edge line
                {
                    if (otherHead.Y == 0 && otherNeck.Y == 1 || otherHead.Y == w - 1 && otherNeck.X == w - 2)
                    {
                        Point possibleMove1 = new() { X = otherHead.X + 1, Y = otherHead.Y };
                        Point possibleMove2 = new() { X = otherHead.X - 1, Y = otherHead.Y };
                        if (IsInBounds(possibleMove1.X, possibleMove1.Y) && IsInBounds(possibleMove2.X, possibleMove2.Y))
                        {
                            Point closestFoodMove1 = FindClosestFood(availableFoods, possibleMove1);
                            Point closestFoodMove2 = FindClosestFood(availableFoods, possibleMove2);
                            if (closestFoodMove1 != null && closestFoodMove2 != null)
                            {
                                int distanceMove1 = ManhattenDistance(possibleMove1.X, possibleMove1.Y, closestFoodMove1.X, closestFoodMove1.Y);
                                int distanceMove2 = ManhattenDistance(possibleMove2.X, possibleMove2.Y, closestFoodMove2.X, closestFoodMove2.Y);
                                otherSnakeMove = distanceMove1 <= distanceMove2 ? possibleMove1 : possibleMove2;
                            }
                        }
                    }
                    else if (otherHead.X == 0 && otherNeck.X == 1 || otherHead.X == h - 1 && otherNeck.X == h - 2)
                    {
                        Point possibleMove1 = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                        Point possibleMove2 = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                        if (IsInBounds(possibleMove1.X, possibleMove1.Y) && IsInBounds(possibleMove2.X, possibleMove2.Y))
                        {
                            Point closestFoodMove1 = FindClosestFood(availableFoods, possibleMove1);
                            Point closestFoodMove2 = FindClosestFood(availableFoods, possibleMove2);
                            if (closestFoodMove1 != null && closestFoodMove2 != null)
                            {
                                int distanceMove1 = ManhattenDistance(possibleMove1.X, possibleMove1.Y, closestFoodMove1.X, closestFoodMove1.Y);
                                int distanceMove2 = ManhattenDistance(possibleMove2.X, possibleMove2.Y, closestFoodMove2.X, closestFoodMove2.Y);
                                otherSnakeMove = distanceMove1 <= distanceMove2 ? possibleMove1 : possibleMove2;
                            }
                        }
                    }
                    else if (myLength > otherLength) //We're longer check is we're chasing him
                    {
                        if (other.Head.Y == w - 1) //Right line
                        {
                            if (other.Head.X + 1 == myHead.X && myHead.Y == w - 2) //Is going up
                            {
                                otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                                isCornorChasing = true;
                            }
                            else if (other.Head.X - 1 == myHead.X && myHead.Y == w - 2) //Is moving down
                            {
                                otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                                isCornorChasing = true;
                            }
                        }
                        else if (other.Head.Y == 0) //Left line
                        {
                            if (other.Head.X + 1 == myHead.X && myHead.Y == 1) //Is going up
                            {
                                otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                                isCornorChasing = true;
                            }
                            else if (other.Head.X - 1 == myHead.X && myHead.Y == 1) //Is moving down
                            {
                                otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                                isCornorChasing = true;
                            }
                        }
                        else if (other.Head.X == 0) //Upper line
                        {
                            if (other.Head.Y + 1 == myHead.Y && myHead.X == 1) //Is going left
                            {
                                otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                                isCornorChasing = true;
                            }
                            else if (other.Head.Y - 1 == myHead.Y && myHead.X == 1) //Is going right
                            {
                                otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                                isCornorChasing = true;
                            }
                        }
                        else if (other.Head.X == h - 1) //Bottom line
                        {
                            if (other.Head.Y + 1 == myHead.Y && myHead.X == h - 2) //Is going left
                            {
                                otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                                isCornorChasing = true;
                            }
                            else if (other.Head.Y - 1 == myHead.Y && myHead.X == h - 2) //Is going right
                            {
                                otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                                isCornorChasing = true;
                            }
                        }
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

                int distanceToOtherSnake = Math.Abs(maxDistance - ManhattenDistance(myHead.X, myHead.Y, otherSnakeMove.X, otherSnakeMove.Y));
                aggresionScore = isCornorChasing ? distanceToOtherSnake * HeuristicConstants.AGGRESSION_VALUE * HeuristicConstants.CORNOR_AGGRESSION_VALUE : distanceToOtherSnake * HeuristicConstants.AGGRESSION_VALUE;
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
                    Point myClosestFood = FindClosestFood(availableFoods, myHead);
                    if (myClosestFood != null)
                    {
                        int distanceToMyClosestFood = ManhattenDistance(myHead.X, myHead.Y, myClosestFood.X, myClosestFood.Y);
                        foodScore = Math.Pow((maxDistance - distanceToMyClosestFood) / 4, 2);
                    }
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
                    Point otherClosestFood = FindClosestFood(availableFoods, otherHead);
                    if (otherClosestFood != null)
                    {
                        int distanceToTheirClosestFood = ManhattenDistance(otherHead.X, otherHead.Y, otherClosestFood.X, otherClosestFood.Y);
                        theirFoodScore = Math.Pow((maxDistance - distanceToTheirClosestFood) / 4, 2);
                    }
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
            Point head = new() { X = me.Head.X, Y = me.Head.Y };
            int cavernSize = FloodFillWithLimit(grid, head, me.Length);
            if (cavernSize >= HeuristicConstants.SAFE_CAVERN_SIZE * me.Length) return 0;
            double floodFillScore = (HeuristicConstants.FLOODFILL_MAX - HeuristicConstants.FLOODFILL_MIN) / Math.Sqrt(HeuristicConstants.SAFE_CAVERN_SIZE * HeuristicConstants.MAX_SNAKE_LENGTH) * Math.Sqrt(cavernSize) - HeuristicConstants.FLOODFILL_MAX;
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

            if (me.Health <= 0)
                mySnakeDead = true;

            if (other.Health <= 0)
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
            foreach (var b in me.Body)
            {
                if (b.X == myHead.X && b.Y == myHead.Y)
                    mySnakeHeadOnCount++;
                if (b.X == otherHead.X && b.Y == otherHead.Y)
                    otherSnakeHeadOnCount++;
            }

            foreach (var b in other.Body)
            {
                if (b.X == myHead.X && b.Y == myHead.Y)
                    mySnakeHeadOnCount++;
                if (b.X == otherHead.X && b.Y == otherHead.Y)
                    otherSnakeHeadOnCount++;
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
                    GameObject[][] clone = CloneGrid(_grid);
                    Point head = new() { X = dx, Y = dy };
                    int score = FloodFillWithLimit(clone, head, me.Length);
                    dx -= me.Head.X;
                    dy -= me.Head.Y;
                    list.Add((dx, dy, score));
                }
            }

            if (list.Count > 0)
            {
                (int x, int y, int freeTiles) = list.OrderByDescending(v => v.freeTiles).First();
                return _dirs[(x, y)];
            }

            return _dir;
        }

        private int FloodFillWithLimit(GameObject[][] grid, Point head, int limit)
        {
            Queue<(int x, int y)> queue = new();
            queue.Enqueue((head.X, head.Y));
            bool[,] isVisited = new bool[grid.Length, grid.First().Length];
            isVisited[head.X, head.Y] = true;
            int count = 1;
            while (queue.Any())
            {
                (int x, int y) = queue.Dequeue();

                if (count >= limit)
                    break;

                foreach ((int x, int y) dir in _dirs.Keys)
                {
                    int dx = dir.x + x, dy = dir.y + y;
                    if (IsMoveableTile(grid, dx, dy) && !isVisited[dx, dy])
                    {
                        isVisited[dx, dy] = true;
                        queue.Enqueue((dx, dy));
                        count++;
                    }
                }
            }
            return count;
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
                (int x, int y, List<(int x, int y)> steps) current = queue.Pop().Value;

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

        private Point FindClosestFood(List<Point> foods, Point head)
        {
            if (foods.Count == 0) return null; //Base case
            else if (foods.Count == 1) return foods.First(); //Base case
            Point bestFood = foods.First();
            int minDistance = ManhattenDistance(bestFood.X, bestFood.Y, head.X, head.Y);
            for (int i = 1; i < foods.Count; i++)
            {
                Point currentFood = foods[i];
                int distance = ManhattenDistance(currentFood.X, currentFood.Y, head.X, head.Y);
                if (minDistance > distance)
                {
                    minDistance = distance;
                    bestFood = currentFood;
                }
            }
            return bestFood;
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
