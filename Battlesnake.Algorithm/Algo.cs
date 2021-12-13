﻿using AlgoKit.Collections.Heaps;
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
        private readonly (int x, int y)[] _moves = new (int x, int y)[4]
        {
            (0, -1), //Left
            (1, 0), //Down
            (0, 1), //Right
            (-1, 0) //Up
        };

        private readonly Stopwatch _watch;
        private bool IsTimeoutThresholdReached => _watch.Elapsed >= TimeSpan.FromMilliseconds(_game.Game.Timeout - 50);

        public Algo(GameStatusDTO game, Direction dir, Stopwatch watch)
        {
            IS_LOCAL = false;
            _rand = new();
            _watch = watch;
            UpdateCoordinates(game);
            _game = game;
            _dir = dir;
            _grid = GenerateGrid();
        }

        public Algo(GameStatusDTO game, GameObject[][] grid)
        {
            IS_LOCAL = true;
            _watch = Stopwatch.StartNew();
            _rand = new();
            _game = game;
            _dir = Direction.LEFT;
            _grid = grid;
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

                _dir = BestAdjacentFloodFill(me);
            }
            catch (Exception e)
            {
                if (Util.IsDebug) WriteDebugMessage(e.Message);
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
                        Direction otherDirection = BestAdjacentFloodFill(other);
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
            if (Util.IsDebug) Print();
            GameObject[][] gridClone = CloneGrid(_grid);
            Print(gridClone);
            Snake meClone = me.Clone();
            Snake otherClone = other.Clone();
            (double score, Direction move) = Minimax(gridClone, meClone, otherClone, HeuristicConstants.MINIMAX_DEPTH);
            if (Util.IsDebug) WriteDebugMessage($"Best score from minimax: {score} -- move to perform: {move}");
            return move;
        }

        private double _currentBestMinimaxScore;
        private Direction _currentBestMinimaxMove;
        private (double score, Direction move) Minimax(GameObject[][] grid, Snake me, Snake other, int depth, bool isMaximizingPlayer = true, int myFoodCount = 0, int otherFoodCount = 0, double alpha = double.MinValue, double beta = double.MaxValue)
        {
            if (IsTimeoutThresholdReached) //Failsafe to handle timeout
            {
                if (Util.IsDebug) WriteDebugMessage($"THRESHOLD! {_watch.Elapsed}");
                return (_currentBestMinimaxScore, _currentBestMinimaxMove);
            }
            
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

            Direction bestMove = Direction.NO_MOVE;
            List<Snake> snakes = new() { me, other };
            Snake currentSnake = isMaximizingPlayer ? me : other;
            double bestMoveScore = isMaximizingPlayer ? double.MinValue : double.MaxValue;
            int prevAppleCount = isMaximizingPlayer ? myFoodCount : otherFoodCount;
            for (int i = 0; i < _moves.Length; i++) //For loop because it's faster in runtime
            {
                (int x, int y) = _moves[i];
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
                            _currentBestMinimaxScore = bestMoveScore;
                            _currentBestMinimaxMove = bestMove;
                        }
                        alpha = Math.Max(alpha, eval.score);
                    }
                    else
                    {
                        if (eval.score < bestMoveScore)
                        {
                            bestMoveScore = eval.score;
                            bestMove = move;
                            _currentBestMinimaxScore = bestMoveScore;
                            _currentBestMinimaxMove = bestMove;
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
            for (int i = 0; i < snakes.Count; i++)
            {
                Snake snake = snakes[i];
                for (int j = 0; j < snake.Body.Count; j++)
                {
                    Point body = snake.Body[i];
                    grid[body.X][body.Y] = GameObject.FLOOR;
                }
            }
        }

        private void ApplySnakesToGrid(GameObject[][] grid, List<Snake> snakes)
        {
            //Add snakes to board
            for (int i = 0; i < snakes.Count; i++)
            {
                Snake snake = snakes[i];
                for (int j = 0; j < snake.Body.Count; j++)
                {
                    Point body = snake.Body[i];
                    grid[body.X][body.Y] = GameObject.BODY;
                }
                grid[snake.Head.X][snake.Head.Y] = GameObject.HEAD;
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

        //Edge methods
        private bool IsOnAnyEdge(Point head) => IsOnRightEdge(head) || IsOnLeftEdge(head) || IsOnBottomEdge(head) || IsOnTopEdge(head);
        private bool IsOnRightEdge(Point head) => head.Y == _game.Board.Width - 1;
        private bool IsOnLeftEdge(Point head) => head.Y == 0;
        private bool IsOnTopEdge(Point head) => head.X == 0;
        private bool IsOnBottomEdge(Point head) => head.X == _game.Board.Height - 1;
        private bool IsAheadOnRightEdgeGoingUp(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.X + 1 == behindHead.X && behindHead.X == aheadNeck.X && me.Head.Y == _game.Board.Width - 2;
        private bool IsAheadOnRightEdgeGoingDown(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.X - 1 == behindHead.X && behindHead.X == aheadNeck.X && me.Head.Y == _game.Board.Width - 2;
        private bool IsAheadOnLeftEdgeGoingUp(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.X + 1 == behindHead.X && behindHead.X == aheadNeck.X && me.Head.Y == 1;
        private bool IsAheadOnLeftEdgeGoingDown(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.X - 1 == behindHead.X && behindHead.X == aheadNeck.X && me.Head.Y == 1;
        private bool IsAheadOnTopEdgeGoingLeft(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.Y + 1 == behindHead.Y && behindHead.Y == aheadNeck.Y && me.Head.X == 1;
        private bool IsAheadOnTopEdgeGoingRight(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.Y - 1 == behindHead.Y && behindHead.Y == aheadNeck.Y && me.Head.X == 1;
        private bool IsAheadOnBottomEdgeGoingLeft(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.Y + 1 == behindHead.Y && behindHead.Y == aheadNeck.Y && me.Head.X == _game.Board.Height - 2;
        private bool IsAheadOnBottomEdgeGoingRight(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.Y - 1 == behindHead.Y && behindHead.Y == aheadNeck.Y && me.Head.X == _game.Board.Height - 2;

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
            Point otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y };
            if (myLength >= otherLength + 2)
            {
                Point otherNeck = other.Body[1];
                (double distance, Point corner) = FindClosestCorner(otherHead);
                if (distance == 0) //Other snake is in a corner
                {
                    if (corner.X == 0 && corner.Y == 0) //Upper left
                    {
                        if (otherNeck.X == 1) //Next move is right
                            otherSnakeMove = new() { X = 0, Y = 1 };
                        else //Next move is down
                            otherSnakeMove = new() { X = 1, Y = 0 };
                    }
                    else if (corner.X == h - 1 && corner.Y == 0) //Lower left
                    {
                        if (otherNeck.X == h - 2) //Next move is right
                            otherSnakeMove = new() { X = h - 1, Y = 1 };
                        else //Next move is up
                            otherSnakeMove = new() { X = h - 2, Y = 0 };
                    }
                    else if (corner.X == 0 && corner.Y == w - 1) //Upper right
                    {
                        if (otherNeck.X == 1) //Next move is left
                            otherSnakeMove = new() { X = 0, Y = w - 2 };
                        else //Next move is down
                            otherSnakeMove = new() { X = 1, Y = w - 1 };
                    }
                    else if (corner.X == h - 1 && corner.Y == w - 1) //Lower right
                    {
                        if (otherNeck.X == h - 2) //Next move is left
                            otherSnakeMove = new() { X = h - 1, Y = w - 2 };
                        else //Next move is up
                            otherSnakeMove = new() { X = h - 2, Y = w - 1 };
                    }
                }
                else if (IsOnAnyEdge(otherHead))
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
                                int distanceMove1 = Util.ManhattenDistance(possibleMove1.X, possibleMove1.Y, closestFoodMove1.X, closestFoodMove1.Y);
                                int distanceMove2 = Util.ManhattenDistance(possibleMove2.X, possibleMove2.Y, closestFoodMove2.X, closestFoodMove2.Y);
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
                                int distanceMove1 = Util.ManhattenDistance(possibleMove1.X, possibleMove1.Y, closestFoodMove1.X, closestFoodMove1.Y);
                                int distanceMove2 = Util.ManhattenDistance(possibleMove2.X, possibleMove2.Y, closestFoodMove2.X, closestFoodMove2.Y);
                                otherSnakeMove = distanceMove1 <= distanceMove2 ? possibleMove1 : possibleMove2;
                            }
                        }
                    }
                    //Other snake is one tile ahead of us and therefor I can cornor trap him because I'm longer
                    else if (IsOnRightEdge(otherHead))
                    {
                        if (IsAheadOnRightEdgeGoingUp(otherHead, myHead, otherNeck, me))
                            otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                        else if (IsAheadOnRightEdgeGoingDown(otherHead, myHead, otherNeck, me))
                            otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                    }
                    else if (IsOnLeftEdge(otherHead))
                    {
                        if (IsAheadOnLeftEdgeGoingUp(otherHead, myHead, otherNeck, me))
                            otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                        else if (IsAheadOnLeftEdgeGoingDown(otherHead, myHead, otherNeck, me))
                            otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                    }
                    else if (IsOnTopEdge(otherHead))
                    {
                        if (IsAheadOnTopEdgeGoingLeft(otherHead, myHead, otherNeck, me))
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                        else if (IsAheadOnTopEdgeGoingRight(otherHead, myHead, otherNeck, me))
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                    }
                    else if (IsOnBottomEdge(otherHead))
                    {
                        if (IsAheadOnBottomEdgeGoingLeft(otherHead, myHead, otherNeck, me))
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                        else if (IsAheadOnBottomEdgeGoingRight(otherHead, myHead, otherNeck, me))
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
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

                int distanceToOtherSnake = Math.Abs(maxDistance - Util.ManhattenDistance(myHead.X, myHead.Y, otherSnakeMove.X, otherSnakeMove.Y));
                aggresionScore = distanceToOtherSnake * HeuristicConstants.AGGRESSION_VALUE;
            }
            else
            {
                if (IsOnAnyEdge(otherHead))
                {
                    Point myNeck = me.Body[1];
                    bool foundPossibleMove = false;
                    //I'm one tile ahead of the other snake and therefor I can cornor trap him
                    if (IsOnRightEdge(otherHead))
                    {
                        if (IsAheadOnRightEdgeGoingUp(myHead, otherHead, myNeck, me))
                        {
                            otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                            foundPossibleMove = true;
                        }
                        else if (IsAheadOnRightEdgeGoingDown(myHead, otherHead, myNeck, me))
                        {
                            otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                            foundPossibleMove = true;
                        }
                    }
                    else if (IsOnLeftEdge(otherHead))
                    {
                        if (IsAheadOnLeftEdgeGoingUp(myHead, otherHead, myNeck, me))
                        {
                            otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                            foundPossibleMove = true;
                        }
                        else if (IsAheadOnLeftEdgeGoingDown(myHead, otherHead, myNeck, me))
                        {
                            otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                            foundPossibleMove = true;
                        }
                    }
                    else if (IsOnTopEdge(otherHead))
                    {
                        if (IsAheadOnTopEdgeGoingLeft(myHead, otherHead, myNeck, me))
                        {
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                            foundPossibleMove = true;
                        }
                        else if (IsAheadOnTopEdgeGoingRight(myHead, otherHead, myNeck, me))
                        {
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                            foundPossibleMove = true;
                        }
                    }
                    else if (IsOnBottomEdge(otherHead))
                    {
                        if (IsAheadOnBottomEdgeGoingLeft(myHead, otherHead, myNeck, me))
                        {
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                            foundPossibleMove = true;
                        }
                        else if (IsAheadOnBottomEdgeGoingRight(myHead, otherHead, myNeck, me))
                        {
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                            foundPossibleMove = true;
                        }
                    }

                    if (foundPossibleMove)
                    {
                        if (!IsInBounds(otherSnakeMove.X, otherSnakeMove.Y)) //Not a valid move
                        {
                            List<Point> safeTiles = SafeTiles(grid, other);
                            int index = _rand.Next(0, safeTiles.Count);
                            otherSnakeMove = safeTiles[index];
                        }

                        int distanceToOtherSnake = Math.Abs(maxDistance - Util.ManhattenDistance(myHead.X, myHead.Y, otherSnakeMove.X, otherSnakeMove.Y));
                        aggresionScore = distanceToOtherSnake * HeuristicConstants.AGGRESSION_VALUE;
                    }
                }
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
                        int distanceToMyClosestFood = Util.ManhattenDistance(myHead.X, myHead.Y, myClosestFood.X, myClosestFood.Y);
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
                        int distanceToTheirClosestFood = Util.ManhattenDistance(otherHead.X, otherHead.Y, otherClosestFood.X, otherClosestFood.Y);
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

            //Voronoi
            double voronoiScore = VoronoiAlgorithm.ChamberHeuristic(grid, me, other) * HeuristicConstants.VORONOI_VALUE;
            score += voronoiScore;

            //Edge
            double edgeScore = 0d;
            double outerBound = HeuristicConstants.EDGE_VALUE_INNER;
            double secondOuterBound = HeuristicConstants.EDGE_VALUE_OUTER;
            //Me -- Bad for being close to the edge
            if (IsOnAnyEdge(myHead))
                edgeScore -= outerBound / 2;
            else if (myHead.X == 1 || myHead.X == h - 2 || myHead.Y == 1 || myHead.Y == w - 2)
                edgeScore -= secondOuterBound / 2;
            //Other -- Good for me if other is close to the edge
            if (IsOnAnyEdge(otherHead))
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
            double round = Math.Round(HeuristicConstants.SAFE_CAVERN_SIZE * me.Length);
            int maxLength = (int)round;
            int cavernSize = BestAdjacentFloodFill(grid, me.Head, maxLength);
            if (cavernSize >= maxLength) return 0;
            double floodFillScore = (HeuristicConstants.FLOODFILL_MAX - HeuristicConstants.FLOODFILL_MIN) / Math.Sqrt(maxLength) * Math.Sqrt(cavernSize) - HeuristicConstants.FLOODFILL_MAX;
            return floodFillScore;
        }

        private List<Point> SafeTiles(GameObject[][] grid, Snake me)
        {
            List<Point> neighbours = new();
            for (int i = 0; i < _moves.Length; i++)
            {
                (int x, int y) = _moves[i];
                int dx = x + me.Head.X, dy = y + me.Head.Y;
                if (IsMoveableTile(grid, dx, dy))
                    neighbours.Add(new Point() { X = dx, Y = dy });
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
            for (int i = 0; i < me.Body.Count; i++)
            {
                Point body = me.Body[i];
                if (body.X == myHead.X && body.Y == myHead.Y)
                    mySnakeHeadOnCount++;

                if (body.X == otherHead.X && body.Y == otherHead.Y)
                    otherSnakeHeadOnCount++;
            }

            for (int i = 0; i < other.Body.Count; i++)
            {
                Point body = other.Body[i];
                if (body.X == myHead.X && body.Y == myHead.Y)
                    mySnakeHeadOnCount++;

                if (body.X == otherHead.X && body.Y == otherHead.Y)
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
            int pow = HeuristicConstants.MINIMAX_DEPTH - remainingDepth - 2; //TODO MAYBE MAKE SURE THIS ISN'T A NEGATIVE NUMBER!
            double futureUncertainty = Math.Pow(HeuristicConstants.FUTURE_UNCERTAINTY_FACOTR, pow);
            return score * futureUncertainty;
        }
        #endregion

        #region Flood fill
        private Direction BestAdjacentFloodFill(Snake me)
        {
            //if (Util.IsDebug) WriteDebugMessage("Using floodfill");
            List<(int x, int y, int freeTiles)> list = new();
            foreach (var (x, y) in _dirs.Keys)
            {
                int dx = me.Head.X + x, dy = me.Head.Y + y;
                if (IsMoveableTile(dx, dy))
                {
                    int score = FloodFillWithLimit(_grid, new() { X = dx, Y = dy }, me.Length);
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

        private int BestAdjacentFloodFill(GameObject[][] grid, Point head, int limit)
        {
            int bestScore = int.MinValue;
            for (int i = 0; i < _moves.Length; i++)
            {
                (int x, int y) = _moves[i];
                int dx = head.X + x, dy = head.Y + y;
                if (IsMoveableTile(dx, dy))
                {
                    int score = FloodFillWithLimit(grid, new() { X = dx, Y = dy }, limit);
                    bestScore = Math.Max(bestScore, score);
                }
            }
            return bestScore;
        }

        private int FloodFillWithLimit(GameObject[][] grid, Point head, int limit)
        {
            int h = grid.Length, w = grid.First().Length;
            Queue<(int x, int y)> queue = new();
            queue.Enqueue((head.X, head.Y));
            bool[,] isVisited = new bool[h, w];
            isVisited[head.X, head.Y] = true;
            int count = 1;
            while (queue.Any())
            {
                (int x, int y) = queue.Dequeue();

                if (count >= limit)
                    return count;

                for (int i = 0; i < _moves.Length; i++)
                {
                    (int x, int y) dir = _moves[i];
                    int dx = dir.x + x, dy = dir.y + y;
                    if (IsMoveableTile(grid, dx, dy) && !isVisited[dx, dy])
                    {
                        queue.Enqueue((dx, dy));
                        isVisited[dx, dy] = true;
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
                    int distance = Util.ManhattenDistance(target.X, target.Y, dx, dy);
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
                        int distance = Util.ManhattenDistance(target.X, target.Y, dx, dy);
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
                    int distance = Util.ManhattenDistance(food.X, food.Y, snake.Head.X, snake.Head.Y);
                    distances[snake.Id].Add((food, distance));
                }
            }

            //if (Util.IsDebug) WriteDebugMessage("Food on board: " + _game.Board.Food.Count);
            if (distances[me.Id].Count == 0) //No food found
                return null;

            foreach (var snake in distances)
                distances[snake.Key] = snake.Value.OrderBy(food => food.distance).ToList(); //Sort distances by shortest distance

            List<(Point food, int distance)> myFoodDistances = distances[me.Id];
            if (Util.IsDebug) 
                WriteDebugMessage(string.Join(", ", myFoodDistances));

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
            int minDistance = Util.ManhattenDistance(bestFood.X, bestFood.Y, head.X, head.Y);
            for (int i = 1; i < foods.Count; i++)
            {
                Point currentFood = foods[i];
                int distance = Util.ManhattenDistance(currentFood.X, currentFood.Y, head.X, head.Y);
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
            int h = _game.Board.Height, w = _game.Board.Width;
            GameObject[][] clone = new GameObject[h][];
            for (int i = 0; i < h; i++)
            {
                clone[i] = new GameObject[w];
                for (int j = 0; j < w; j++)
                    clone[i][j] = grid[i][j];
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

        public void Print(GameObject[][] grid = null)
        {
            if (grid == null) grid = _grid;
            if (IS_LOCAL)
            {
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
            for (int i = 0; i < game.Board.Food.Count; i++)
            {
                Point item = game.Board.Food[i];
                item.Y = h - item.Y;
                int temp = item.X;
                item.X = item.Y;
                item.Y = temp;
            }

            for (int i = 0; i < game.Board.Snakes.Count; i++)
            {
                Snake s = game.Board.Snakes[i];
                s.Head.Y = h - s.Head.Y;
                int temp = s.Head.X;
                s.Head.X = s.Head.Y;
                s.Head.Y = temp;
                for (int j = 0; j < s.Body.Count; j++)
                {
                    Point b = s.Body[j];
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
            for (int i = 0; i < game.You.Body.Count; i++)
            {
                Point b = game.You.Body[i];
                b.Y = h - b.Y;
                int temp = b.X;
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
