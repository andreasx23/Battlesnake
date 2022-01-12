using AlgoKit.Collections.Heaps;
using Battlesnake.Algorithm.Structs;
using Battlesnake.DTOModel;
using Battlesnake.Enum;
using Battlesnake.Model;
using Battlesnake.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private bool IsTimeoutThresholdReached => _watch.Elapsed >= TimeSpan.FromMilliseconds(_game.Game.Timeout - 75);
        private readonly State _state;
        private double _hit = 0d, _noHit = 0d, _goodHit = 0d;

        public Algo(GameStatusDTO game, Direction dir, Stopwatch watch)
        {
            IS_LOCAL = false;
            _rand = new();
            _watch = watch;
            UpdateCoordinates(game);
            _game = game;
            _dir = dir;
            _grid = GenerateGrid();
            ZobristHash.InitZobristHash(game.Board.Height, game.Board.Width);
            _state = new State(_grid);
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

        public Direction CalculateNextMove(Snake me)
        {
            try
            {
                if (_game.Board.Snakes.Count == 2)
                {
                    Snake other = _game.Board.Snakes.First(s => s.Id != me.Id);
                    Direction iterativeDeepening = ParallelIterativeDeepening(me, other);
                    if (iterativeDeepening != Direction.NO_MOVE)
                    {
                        _dir = iterativeDeepening;
                        return _dir;
                    }
                }
                else
                {
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
            State stateClone = _state.DeepClone();
            Snake meClone = me.Clone();
            Snake otherClone = other.Clone();
            (double score, Direction move) = Minimax(stateClone, meClone, otherClone, stateClone.MAX_DEPTH);
            if (Util.IsDebug) WriteDebugMessage($"Best score from minimax: {score} -- move to perform: {move}");
            return move;
        }

        private bool _searchCutOff = false;
        //https://github.com/nealyoung/CS171/blob/master/AI.java
        //https://stackoverflow.com/questions/41756443/how-to-implement-iterative-deepening-with-alpha-beta-pruning
        //https://stackoverflow.com/questions/16235923/alpha-beta-search-iterative-deepening-refutation-table?rq=1
        //https://stackoverflow.com/questions/29990116/alpha-beta-prunning-with-transposition-table-iterative-deepening?rq=1
        //https://stackoverflow.com/questions/27606175/can-a-transposition-table-cause-search-instability?noredirect=1&lq=1
        private Direction IterativeDeepening(Snake me, Snake other)
        {
            if (Util.IsDebug) Print();
            State stateClone = _state.DeepClone();
            Snake meClone = me.Clone();
            Snake otherClone = other.Clone();

            double bestScore = double.MinValue;
            Direction bestMove = Direction.NO_MOVE;
            int depth = 2;
            double prevHit = _hit;
            double prevNoHit = _noHit;
            double prevGoodHit = _goodHit;
            while (true)
            {
                if (IsTimeoutThresholdReached)
                {
                    depth -= 2;
                    break;
                }

                stateClone.MAX_DEPTH = depth;
                (double score, Direction move) = Minimax(stateClone, meClone, otherClone, depth);

                if (_searchCutOff)
                    break;

                if (Util.IsDebug)
                    Debug.WriteLine(score + " " + move + " " + depth);

                bestScore = score;
                bestMove = move;

                prevHit = _hit;
                prevNoHit = _noHit;
                prevGoodHit = _goodHit;

                depth += 2;
            }

            if (Util.IsDebug)
            {
                double total = prevHit + prevNoHit + prevGoodHit;
                WriteDebugMessage($"No hit: {prevNoHit} + hit: {prevHit} + good hit: {prevGoodHit} = Total: {total} -- Hit procentage: {prevHit / total * 100} Good hit procentage: {prevGoodHit / total * 100} Total success procentage: {(prevHit + prevGoodHit) / total * 100}");
                Debug.WriteLine($"Depth searched to: {depth}, a score of: {bestScore} with move: {bestMove}");
            }

            return bestMove;
        }

        private Direction ParallelIterativeDeepening(Snake me, Snake other)
        {
            if (Util.IsDebug) Print();

            double prevHit = _hit;
            double prevNoHit = _noHit;
            double prevGoodHit = _goodHit;
            ConcurrentBag<(double score, Direction move, int depth)> scores = new();
            Parallel.For(0, 4, i =>
            {
                State StateClone = _state.DeepClone();
                StateClone.MAX_DEPTH = 2 * (i + 1);
                Snake meClone = me.Clone();
                Snake otherClone = other.Clone();
                while (true)
                {
                    if (IsTimeoutThresholdReached)
                        break;

                    /*
                     * https://www.chessprogramming.org/Lazy_SMP
                     * https://chess.stackexchange.com/questions/35257/chess-engine-using-lazysmp
                     * 
                     * https://stackoverflow.com/questions/70303310/why-does-increasing-the-hash-table-size-for-a-chess-engine-also-drastically-inc
                     */
                    (double score, Direction move) = Minimax(StateClone, meClone, otherClone, StateClone.MAX_DEPTH);

                    if (_searchCutOff)
                        break;

                    scores.Add((score, move, StateClone.MAX_DEPTH));
                    StateClone.MAX_DEPTH += 2;
                    prevHit = _hit;
                    prevNoHit = _noHit;
                    prevGoodHit = _goodHit;
                }
            });

            int depth = -1;
            double bestScore = double.MinValue;
            Direction bestMove = Direction.NO_MOVE;
            foreach (var item in scores)
            {
                if (item.depth > depth || item.depth == depth && item.score > bestScore)
                {
                    depth = item.depth;
                    bestScore = item.score;
                    bestMove = item.move;
                }
            }

            if (Util.IsDebug)
            {
                double total = prevHit + prevNoHit + prevGoodHit;
                WriteDebugMessage($"No hit: {prevNoHit} + hit: {prevHit} + good hit: {prevGoodHit} = Total: {total} -- Hit procentage: {prevHit / total * 100} Good hit procentage: {prevGoodHit / total * 100} Total success procentage: {(prevHit + prevGoodHit) / total * 100}");
                Debug.WriteLine($"Depth searched to: {depth}, a score of: {bestScore} with move: {bestMove}");
            }

            return bestMove;
        }

        //http://fierz.ch/strategy2.htm#searchenhance -- HashTables
        //http://people.csail.mit.edu/plaat/mtdf.html#abmem        
        private readonly ConcurrentDictionary<int, TransporationValue> _transportationTable = new();
        private (double score, Direction move) Minimax(State state, Snake me, Snake other, int depth, bool isMaximizingPlayer = true, int myFoodCount = 0, int otherFoodCount = 0, double alpha = double.MinValue, double beta = double.MaxValue)
        {
            if (IsTimeoutThresholdReached)
            {
                _searchCutOff = true;
                if (Util.IsDebug) WriteDebugMessage($"THRESHOLD! {_watch.Elapsed}");
                return (0, Direction.NO_MOVE);
            }

            (int x, int y)[] moves = new (int x, int y)[4]
            {
                (0, -1), //Left
                (1, 0), //Down
                (0, 1), //Right
                (-1, 0) //Up
            };
            if (_transportationTable.TryGetValue(state.Key, out TransporationValue value))
            {
                if (value.Depth >= depth)
                {
                    if (value.LowerBound >= beta)
                    {
                        if (Util.IsDebug) _goodHit++;
                        return (value.LowerBound, value.Move);
                    }
                    else if (value.UpperBound <= alpha)
                    {
                        if (Util.IsDebug) _goodHit++;
                        return (value.UpperBound, value.Move);
                    }
                }
                if (Util.IsDebug) _hit++;
                (int x, int y) temp = moves[0];
                moves[0] = moves[value.MoveIndex];
                moves[value.MoveIndex] = temp;
            }
            else if (Util.IsDebug)
                _noHit++;

            if (depth == 0)
            {
                double evaluatedState = EvaluateState(state, me, other, depth, myFoodCount, otherFoodCount);
                return (evaluatedState, Direction.NO_MOVE);
            }

            if (isMaximizingPlayer) //Only evaluate if game is over when it's my turn because it takes two depths for a turn
            {
                (double score, bool isGameOver) = EvaluateIfGameIsOver(me, other, depth, state.MAX_DEPTH);
                if (isGameOver) return (score, Direction.NO_MOVE);
            }

            Direction bestMove = Direction.NO_MOVE;
            int moveIndex = 0;
            Snake[] snakes = new Snake[2] { me, other };
            Snake currentSnake = isMaximizingPlayer ? me : other;
            double bestMoveScore = isMaximizingPlayer ? double.MinValue : double.MaxValue;
            int currentFoodCount = isMaximizingPlayer ? myFoodCount : otherFoodCount;
            for (int i = 0; i < moves.Length; i++) //For loop because it's faster in runtime
            {
                if (IsTimeoutThresholdReached) //Halt possible new entries
                {
                    _searchCutOff = true;
                    if (Util.IsDebug) WriteDebugMessage($"THRESHOLD! {_watch.Elapsed}");
                    return (0, Direction.NO_MOVE);
                }

                (int x, int y) = moves[i];
                int dx = x + currentSnake.Head.X, dy = y + currentSnake.Head.Y;
                if (IsInBounds(dx, dy))
                {
                    int key = state.Key;
                    //Store values for updating hash
                    Point oldHead = new() { X = currentSnake.Head.X, Y = currentSnake.Head.Y };
                    Point oldNeck = new() { X = currentSnake.Body[1].X, Y = currentSnake.Body[1].Y };
                    Point oldTail = new() { X = currentSnake.Body.Last().X, Y = currentSnake.Body.Last().Y };
                    //Change state of the game
                    GameObject destinationTile = state.Grid[dx][dy];
                    int prevHp = currentSnake.Health;
                    int prevLength = currentSnake.Length;
                    currentSnake.Health -= _game.Game.Ruleset.Settings.HazardDamagePerTurn;
                    bool isFoodTile = IsFoodTile(state.Grid, dx, dy);
                    if (isFoodTile)
                    {
                        Snake temp = !isMaximizingPlayer ? me : other;
                        if (dx != temp.Head.X && dy != temp.Head.Y || currentSnake.Length > temp.Length)
                        {
                            currentSnake.Length++;
                            currentSnake.Health = HeuristicConstants.MAX_HEALTH;
                            currentFoodCount++;
                        }
                    }
                    //Move the snake
                    state.MoveSnakeForward(currentSnake, x, y, isFoodTile);
                    state.UpdateSnakesToGrid(snakes);
                    //Store values for updating hash
                    Point newHead = new() { X = currentSnake.Head.X, Y = currentSnake.Head.Y };
                    Point newTail = new() { X = currentSnake.Body.Last().X, Y = currentSnake.Body.Last().Y };
                    state.Key = ZobristHash.Instance.UpdateKeyForward(state.Key, oldNeck, oldHead, oldTail, newHead, newTail, destinationTile);
                    //Execute minimax
                    (double score, Direction move) = Minimax(state: state,
                                                                    me: isMaximizingPlayer ? currentSnake : me,
                                                                    other: !isMaximizingPlayer ? currentSnake : other,
                                                                    depth: depth - 1,
                                                                    isMaximizingPlayer: !isMaximizingPlayer,
                                                                    myFoodCount: isMaximizingPlayer ? currentFoodCount : myFoodCount,
                                                                    otherFoodCount: !isMaximizingPlayer ? currentFoodCount : otherFoodCount,
                                                                    alpha: alpha,
                                                                    beta: beta);
                    if (!IsTimeoutThresholdReached) //Only clear if timeout is not reached
                    {
                        //Move the snake back
                        state.MoveSnakeBackward(currentSnake, oldTail, isFoodTile, destinationTile);
                        state.UpdateSnakesToGrid(snakes);
                        //Revert changes made doing previous state
                        currentSnake.Health = prevHp;
                        currentSnake.Length = prevLength;
                        //Revert changes made to the key
                        state.Key = ZobristHash.Instance.UpdateKeyBackward(state.Key, oldNeck, oldHead, oldTail, newHead, newTail, destinationTile);
                        Debug.Assert(key == state.Key);
                    }

                    if (isMaximizingPlayer)
                    {
                        if (score > bestMoveScore)
                        {
                            bestMoveScore = score;
                            bestMove = GetMove(x, y);
                            moveIndex = i;
                        }
                        alpha = Math.Max(alpha, score);
                    }
                    else
                    {
                        if (score < bestMoveScore)
                        {
                            bestMoveScore = score;
                            bestMove = GetMove(x, y);
                            moveIndex = i;
                        }
                        beta = Math.Min(beta, score);
                    }
                    if (beta <= alpha) break;
                }
            }

            double lowerBound = double.MinValue, upperbound = double.MaxValue;
            if (bestMoveScore <= alpha) upperbound = bestMoveScore;
            else if (bestMoveScore > alpha && bestMoveScore < beta)
            {
                upperbound = bestMoveScore;
                lowerBound = bestMoveScore;
            }
            else if (bestMoveScore >= beta) lowerBound = bestMoveScore;
            _transportationTable.TryAdd(state.Key, new() { Move = bestMove, MoveIndex = moveIndex, Depth = depth, LowerBound = lowerBound, UpperBound = upperbound }); //Should use TryAdd instead of containskey since containskey isn't thread safe

            return (bestMoveScore, bestMove);
        }

        private static Direction GetMove(int x, int y)
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
        private static bool IsOnLeftEdge(Point head) => head.Y == 0;
        private static bool IsOnTopEdge(Point head) => head.X == 0;
        private bool IsOnBottomEdge(Point head) => head.X == _game.Board.Height - 1;
        private bool IsAheadOnRightEdgeGoingUp(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.X + 1 == behindHead.X && behindHead.X == aheadNeck.X && me.Head.Y == _game.Board.Width - 2;
        private bool IsAheadOnRightEdgeGoingDown(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.X - 1 == behindHead.X && behindHead.X == aheadNeck.X && me.Head.Y == _game.Board.Width - 2;
        private static bool IsAheadOnLeftEdgeGoingUp(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.X + 1 == behindHead.X && behindHead.X == aheadNeck.X && me.Head.Y == 1;
        private static bool IsAheadOnLeftEdgeGoingDown(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.X - 1 == behindHead.X && behindHead.X == aheadNeck.X && me.Head.Y == 1;
        private static bool IsAheadOnTopEdgeGoingLeft(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.Y + 1 == behindHead.Y && behindHead.Y == aheadNeck.Y && me.Head.X == 1;
        private static bool IsAheadOnTopEdgeGoingRight(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.Y - 1 == behindHead.Y && behindHead.Y == aheadNeck.Y && me.Head.X == 1;
        private bool IsAheadOnBottomEdgeGoingLeft(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.Y + 1 == behindHead.Y && behindHead.Y == aheadNeck.Y && me.Head.X == _game.Board.Height - 2;
        private bool IsAheadOnBottomEdgeGoingRight(Point aheadHead, Point behindHead, Point aheadNeck, Snake me) => aheadHead.Y - 1 == behindHead.Y && behindHead.Y == aheadNeck.Y && me.Head.X == _game.Board.Height - 2;

        private double EvaluateState(State state, Snake me, Snake other, int remainingDepth, int myFoodCount, int otherFoodCount)
        {
            int h = state.Grid.Length, w = state.Grid.First().Length;
            double score = 0d;
            Point myHead = me.Head;
            Point otherHead = other.Head;
            int myLength = me.Length;
            int otherLength = other.Length;
            int maxDistance = h + w;
            List<Point> availableFoods = GetFoodFromGrid(state.Grid);

            //----- Is game over -----
            (double score, bool isGameOver) evaluateIfGameIsOver = EvaluateIfGameIsOver(me, other, remainingDepth, state.MAX_DEPTH);
            if (evaluateIfGameIsOver.isGameOver)
                return evaluateIfGameIsOver.score;

            //----- Aggresion -----
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
                            Point closestFoodMove1 = FindClosestFoodUsingManhattenDistance(availableFoods, possibleMove1);
                            Point closestFoodMove2 = FindClosestFoodUsingManhattenDistance(availableFoods, possibleMove2);
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
                            Point closestFoodMove1 = FindClosestFoodUsingManhattenDistance(availableFoods, possibleMove1);
                            Point closestFoodMove2 = FindClosestFoodUsingManhattenDistance(availableFoods, possibleMove2);
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
                    otherSnakeMove = RandomSafeMove(state.Grid, other);

                int manhattenDistanceToOtherSnake = Util.ManhattenDistance(myHead.X, myHead.Y, otherSnakeMove.X, otherSnakeMove.Y);
                int distanceToOtherSnake = Math.Abs(maxDistance - manhattenDistanceToOtherSnake);
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
                            otherSnakeMove = RandomSafeMove(state.Grid, other);

                        int manhattenDistanceToOtherSnake = Util.ManhattenDistance(myHead.X, myHead.Y, otherSnakeMove.X, otherSnakeMove.Y);
                        int distanceToOtherSnake = Math.Abs(maxDistance - manhattenDistanceToOtherSnake);
                        aggresionScore = distanceToOtherSnake * HeuristicConstants.AGGRESSION_VALUE;
                    }
                }
            }
            score += aggresionScore;

            //----- Voronoi & Food -----
            (int score, int ownedFoodDepth) myVoronoi = VoronoiAlgorithm.VoronoiStateHeuristic(state.Grid, me, other);
            score += myVoronoi.score * HeuristicConstants.VORONOI_VALUE;

            double myFoodScore = 0d;
            if (availableFoods.Count > 0)
            {
                int ownedFoodDistance = myVoronoi.ownedFoodDepth;
                if (ownedFoodDistance == -1) //We don't control any food
                {
                    ownedFoodDistance = FindClosestFoodDistanceUsingBFS(state.Grid, myHead);
                    if (ownedFoodDistance == -1)
                    {
                        Point myClosestFood = FindClosestFoodUsingManhattenDistance(availableFoods, myHead);
                        ownedFoodDistance = Util.ManhattenDistance(myHead.X, myHead.Y, myClosestFood.X, myClosestFood.Y);
                    }
                }
                if (me.Health < ownedFoodDistance)
                    return -10000d;
                double rope = me.Health - ownedFoodDistance;
                myFoodScore = HeuristicConstants.MY_FOOD_VALUE * Math.Atan(rope / HeuristicConstants.ATAN_VALUE);
            }
            score += myFoodScore;

            (int score, int ownedFoodDepth) otherVoronoi = VoronoiAlgorithm.VoronoiStateHeuristic(state.Grid, other, me);
            score += -1d * otherVoronoi.score * HeuristicConstants.VORONOI_VALUE;

            double theirFoodScore = 0d;
            if (availableFoods.Count > 0)
            {
                int ownedFoodDistance = otherVoronoi.ownedFoodDepth;
                if (ownedFoodDistance == -1) //We don't control any food
                {
                    ownedFoodDistance = FindClosestFoodDistanceUsingBFS(state.Grid, otherHead);
                    if (ownedFoodDistance == -1)
                    {
                        Point otherClosestFood = FindClosestFoodUsingManhattenDistance(availableFoods, otherHead);
                        ownedFoodDistance = Util.ManhattenDistance(otherHead.X, otherHead.Y, otherClosestFood.X, otherClosestFood.Y);
                    }
                }
                if (other.Health < ownedFoodDistance)
                    return 10000d;
                double rope = other.Health - ownedFoodDistance;
                theirFoodScore = HeuristicConstants.OTHER_FOOD_VALUE * Math.Atan(rope / HeuristicConstants.ATAN_VALUE);
            }
            score += theirFoodScore;

            //----- Flood fill -----
            double myFloodFillScore = CalculateFloodfillScore(state.Grid, me) * HeuristicConstants.MY_FLOODFILL_VALUE;
            double otherFloodFillScore = -1d * CalculateFloodfillScore(state.Grid, other) * HeuristicConstants.OTHER_FLOODFILL_VALUE;
            double floodFillScore = myFloodFillScore + otherFloodFillScore;
            score += floodFillScore;

            //----- Edge -----
            double edgeScore = 0d;
            //Me -- Bad for being close to the edge
            if (IsOnAnyEdge(myHead))
                edgeScore -= HeuristicConstants.EDGE_VALUE_INNER / 2;
            else if (IsOnAnyLineSecondFromEdge(h, w, myHead))
                edgeScore -= HeuristicConstants.EDGE_VALUE_OUTER / 2;

            //Other -- Good for me if other is close to the edge
            if (IsOnAnyEdge(otherHead))
                edgeScore += HeuristicConstants.EDGE_VALUE_INNER / 2;
            else if (IsOnAnyLineSecondFromEdge(h, w, otherHead))
                edgeScore += HeuristicConstants.EDGE_VALUE_OUTER / 2;
            score += edgeScore;

            //----- Center -----
            double centerScore = 0d;
            //Good for me if I'm close to center
            if (InnerCenter(myHead))
                centerScore += HeuristicConstants.CENTER_VALUE_INNER / 2;
            else if (OuterCenter(myHead))
                centerScore += HeuristicConstants.CENTER_VALUE_OUTER / 2;

            //Bad for me if other is close to center
            if (InnerCenter(otherHead))
                centerScore -= HeuristicConstants.CENTER_VALUE_INNER / 2;
            else if (OuterCenter(otherHead))
                centerScore -= HeuristicConstants.CENTER_VALUE_OUTER / 2;
            score += centerScore;

            return score;
        }

        private static bool IsOnAnyLineSecondFromEdge(int height, int width, Point myHead) => myHead.X == 1 || myHead.X == height - 2 || myHead.Y == 1 || myHead.Y == width - 2;
        private static bool InnerCenter(Point head) => head.X >= 3 && head.X <= 7 && head.Y >= 3 && head.Y <= 7;
        private static bool OuterCenter(Point head) => head.X == 2 && head.Y >= 2 && head.Y <= 8 ||  //Upper center
                                                head.X == 8 && head.Y >= 2 && head.Y <= 8 || //Lower center
                                                head.Y == 2 && head.X >= 2 && head.X <= 8 || //Left center
                                                head.Y == 8 && head.X >= 2 && head.X <= 8;   //Right center

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
            double floodFillScore = (HeuristicConstants.MAX_FLOODFILL_SCORE - HeuristicConstants.MIN_FLOODFILL_SCORE) / Math.Sqrt(HeuristicConstants.SAFE_CAVERN_SIZE * HeuristicConstants.MAX_SNAKE_LENGTH) * Math.Sqrt(cavernSize) - HeuristicConstants.MAX_FLOODFILL_SCORE;
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

        private Point RandomSafeMove(GameObject[][] grid, Snake me)
        {
            List<Point> safeTiles = SafeTiles(grid, me);
            if (safeTiles.Count == 0) return new() { X = Math.Abs(me.Head.X - 1), Y = me.Head.Y }; //Up or down if out of bounds
            int index = _rand.Next(0, safeTiles.Count);
            return safeTiles[index];
        }

        private (double score, bool isGameOver) EvaluateIfGameIsOver(Snake me, Snake other, int remainingDepth, int maxDepth)
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
            if (_game.Turn != 0)
            {
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
                score = AdjustForFutureUncetainty(-1000, remainingDepth, maxDepth);
            else if (mySnakeMaybeDead && otherSnakeMaybeDead)
                score = AdjustForFutureUncetainty(-750, remainingDepth, maxDepth);
            else if (mySnakeMaybeDead)
                score = AdjustForFutureUncetainty(-500, remainingDepth, maxDepth);
            else if (otherSnakeMaybeDead)
                score = AdjustForFutureUncetainty(500, remainingDepth, maxDepth);
            else if (otherSnakeDead)
                score = AdjustForFutureUncetainty(1000, remainingDepth, maxDepth);
            else
                score = AdjustForFutureUncetainty(0, remainingDepth, maxDepth);

            bool isGameOver = false;
            if (mySnakeDead || otherSnakeDead || mySnakeMaybeDead || otherSnakeMaybeDead) isGameOver = true;

            //Node depth score -- https://levelup.gitconnected.com/improving-minimax-performance-fc82bc337dfd section 9
            score += remainingDepth;

            return (score, isGameOver);
        }

        private static double AdjustForFutureUncetainty(double score, int remainingDepth, int maxDepth)
        {
            int pow = maxDepth - remainingDepth - 2;
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

        private static Point FindClosestFoodUsingManhattenDistance(List<Point> foods, Point head)
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

        private int FindClosestFoodDistanceUsingBFS(GameObject[][] grid, Point head)
        {
            Queue<(int x, int y, int steps)> queue = new();
            int h = grid.Length, w = grid.First().Length;
            bool[,] isVisited = new bool[h, w];
            foreach (var (x, y) in Neighbours(grid, head.X, head.Y))
            {
                queue.Enqueue((x, y, 1));
                isVisited[x, y] = true;
            }

            while (queue.Any())
            {
                (int x, int y, int steps) = queue.Dequeue();

                if (IsFoodTile(grid, x, y))
                    return steps;

                int next = steps + 1;
                List<(int x, int y)> neighbours = Neighbours(grid, x, y);
                for (int i = 0; i < neighbours.Count; i++)
                {
                    (int x, int y) neighbour = neighbours[i];
                    if (!isVisited[neighbour.x, neighbour.y])
                    {
                        isVisited[neighbour.x, neighbour.y] = true;
                        queue.Enqueue((neighbour.x, neighbour.y, next));
                    }
                }
            }
            return -1;
        }

        //https://github.com/aleksiy325/snek-two/blob/da589b945e347c5178f6cc0c8b190a28651cce50/src/common/game_state.cpp -- maybe implement bfsFood from here
        //This implementation is bugged and wrong currently
        private int FindClosestFoodDistanceUsingBFSWithDepth(GameObject[][] grid, Snake me, Snake other, Point head)
        {
            int depth = 0, h = grid.Length, w = grid.First().Length;
            (int x, int y, int steps) DEPTH_MARK = (-1, -1, -1);
            Queue<(int x, int y, int steps)> queue = new();
            bool[,] isVisited = new bool[h, w];
            foreach (var (x, y) in Neighbours(grid, head.X, head.Y))
            {
                queue.Enqueue((x, y, 1));
                isVisited[x, y] = true;
            }
            queue.Enqueue(DEPTH_MARK);

            int steps = -1;
            while (queue.Any())
            {
                (int x, int y, int steps) current = queue.Dequeue();

                if (current == DEPTH_MARK)
                {
                    depth++;
                    queue.Enqueue(DEPTH_MARK);
                    if (queue.Peek() == DEPTH_MARK)
                        break;
                }
                else if (IsFoodTile(grid, current.x, current.y))
                {
                    steps = current.steps;
                    break;
                }
                else
                {
                    int next = current.steps + 1;
                    foreach (var (x, y) in _moves)
                    {
                        int dx = current.x + x, dy = current.y + y;
                        if (IsSafe(grid, me, other, dx, dy, depth) && !isVisited[dx, dy])
                        {
                            isVisited[dx, dy] = true;
                            queue.Enqueue((dx, dx, next));
                        }
                    }
                }
            }

            return steps;
        }
        #endregion

        #region Helper functions
        private List<(int x, int y)> Neighbours(GameObject[][] grid, int x, int y)
        {
            List<(int x, int y)> neighbours = new();
            for (int i = 0; i < _moves.Length; i++)
            {
                (int x, int y) move = _moves[i];
                int dx = move.x + x, dy = move.y + y;
                if (IsMoveableTile(grid, dx, dy))
                    neighbours.Add((dx, dy));
            }
            return neighbours;
        }

        private void WriteDebugMessage(string message)
        {
            if (IS_LOCAL)
                Console.WriteLine(message);
            else
                Debug.WriteLine(message);
        }

        private bool WillBeUnocupied(GameObject[][] grid, Snake me, Snake other, int x, int y, int distance)
        {
            bool willBeUnocupied = true;
            if (!IsMoveableTile(grid, x, y))
            {
                Snake occupant = me.Body.Any(p => p.X == x && p.Y == y) ? me : other.Body.Any(p => p.X == x && p.Y == y) ? other : null;
                if (occupant == null)
                {
                    Debug.Assert(false);
                    return false;
                }
                int turnsOcupied = -1, n = occupant.Body.Count;
                for (int i = 0; i < n; i++)
                {
                    Point b = occupant.Body[i];
                    if (b.X == x && b.Y == y)
                    {
                        turnsOcupied = i;
                        break;
                    }
                }
                turnsOcupied = n - turnsOcupied + 1;
                willBeUnocupied = turnsOcupied <= distance;
            }
            return willBeUnocupied;
        }

        private bool IsSafe(GameObject[][] grid, Snake me, Snake other, int x, int y, int distance)
        {
            return IsInBounds(x, y) && WillBeUnocupied(grid, me, other, x, y, distance);
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

        private static bool IsHeadCollision(Snake me, Snake other)
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

        private static void UpdateCoordinates(GameStatusDTO game)
        {
            int h = game.Board.Height - 1;
            for (int i = 0; i < game.Board.Food.Count; i++)
            {
                Point food = game.Board.Food[i];
                food.Y = h - food.Y;
                int temp = food.X;
                food.X = food.Y;
                food.Y = temp;
            }

            for (int i = 0; i < game.Board.Snakes.Count; i++)
            {
                Snake snake = game.Board.Snakes[i];
                snake.Head.Y = h - snake.Head.Y;
                int temp = snake.Head.X;
                snake.Head.X = snake.Head.Y;
                snake.Head.Y = temp;
                for (int j = 0; j < snake.Body.Count; j++)
                {
                    Point body = snake.Body[j];
                    body.Y = h - body.Y;
                    temp = body.X;
                    body.X = body.Y;
                    body.Y = temp;
                }
            }

            game.You.Head.Y = h - game.You.Head.Y;
            int temp1 = game.You.Head.X;
            game.You.Head.X = game.You.Head.Y;
            game.You.Head.Y = temp1;
            for (int i = 0; i < game.You.Body.Count; i++)
            {
                Point body = game.You.Body[i];
                body.Y = h - body.Y;
                int temp = body.X;
                body.X = body.Y;
                body.Y = temp;
            }
        }

        private static Direction MoveLeft(Snake snake)
        {
            if (snake.Direction != Direction.RIGHT)
                return Direction.LEFT;
            else
                return Direction.NO_MOVE;
        }

        private static Direction MoveRight(Snake snake)
        {
            if (snake.Direction != Direction.LEFT)
                return Direction.RIGHT;
            else
                return Direction.NO_MOVE;
        }

        private static Direction MoveUp(Snake snake)
        {
            if (snake.Direction != Direction.DOWN)
                return Direction.UP;
            else
                return Direction.NO_MOVE;
        }

        private static Direction MoveDown(Snake snake)
        {
            if (snake.Direction != Direction.UP)
                return Direction.DOWN;
            else
                return Direction.NO_MOVE;
        }
        #endregion
    }
}
