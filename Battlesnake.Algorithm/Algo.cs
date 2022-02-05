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
        private bool IsTimeoutThresholdReached => _watch.Elapsed >= TimeSpan.FromMilliseconds(_game.Game.Timeout - 80);
        private readonly State _state;
        private double _hit = 0d, _noHit = 0d, _goodHit = 0d;
        private bool _searchCutOff = false;
        private readonly GameMode _gameMode = GameMode.NORMAL;
        private readonly HashSet<(int x, int y)> _hazardSpots = new();

        public Algo(GameStatusDTO game, Direction dir, Stopwatch watch)
        {
            _gameMode = game.Game.Ruleset.Name.ToLower() switch
            {
                "wrapped" => GameMode.WRAPPED,
                "royale" => GameMode.ROYALE,
                "constrictor" => GameMode.CONSTRICTOR,
                _ => GameMode.NORMAL,
            };
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

        public Direction CalculateNextMove()
        {
            try
            {
                //Direction iterativeDeepening = MaxNParallelIterativeDeepening();
                Direction iterativeDeepening = MaxNIterativeDeepening();
                if (iterativeDeepening != Direction.NO_MOVE)
                {
                    _dir = iterativeDeepening;
                    return _dir;
                }
                _dir = BestAdjacentFloodFill(_game.You);
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

        #region Mini max with alpha beta pruning
        private Direction Minimax(Snake me, Snake other)
        {
            if (Util.IsDebug) Print();
            State stateClone = _state.DeepClone();
            Snake meClone = me.Clone();
            Snake otherClone = other.Clone();
            (double score, Direction move) = MinimaxWithAlphaBeta(stateClone, meClone, otherClone, stateClone.MAX_DEPTH);
            if (Util.IsDebug) WriteDebugMessage($"Best score from minimax: {score} -- move to perform: {move}");
            return move;
        }

        //https://github.com/nealyoung/CS171/blob/master/AI.java
        //https://stackoverflow.com/questions/41756443/how-to-implement-iterative-deepening-with-alpha-beta-pruning
        //https://stackoverflow.com/questions/16235923/alpha-beta-search-iterative-deepening-refutation-table?rq=1
        //https://stackoverflow.com/questions/29990116/alpha-beta-prunning-with-transposition-table-iterative-deepening?rq=1
        //https://stackoverflow.com/questions/27606175/can-a-transposition-table-cause-search-instability?noredirect=1&lq=1
        private Direction MinimaxIterativeDeepening(Snake me, Snake other)
        {
            if (Util.IsDebug) Print();
            State stateClone = _state.DeepClone();
            Snake meClone = me.Clone();
            Snake otherClone = other.Clone();

            double bestScore = double.MinValue;
            Direction bestMove = Direction.NO_MOVE;
            int depth = 4;
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
                (double score, Direction move) = MinimaxWithAlphaBeta(stateClone, meClone, otherClone, depth);

                if (_searchCutOff)
                    break;

                if (Util.IsDebug)
                    Debug.WriteLine(score + " " + move + " " + depth);

                bestScore = score;
                bestMove = move;

                prevHit = _hit;
                prevNoHit = _noHit;
                prevGoodHit = _goodHit;

                if (depth == 4)
                    break;

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

        //https://play.battlesnake.com/g/befbe2fa-ae56-421b-a740-cdaf2d292eca/?turn=231 BUG
        private Direction MaxNIterativeDeepening()
        {
            if (Util.IsDebug)
                Print();

            State state = _state;//.DeepClone();
            int amountOfSnakes = _game.Board.Snakes.Count;
            List<Snake> snakes = new();
            List<bool> hasSnakeEaten = new();
            List<int> foodCounts = new();
            for (int i = 0; i < amountOfSnakes; i++)
            {
                Snake snake = _game.Board.Snakes[i];//.Clone();
                if (snake.Id == _game.You.Id)
                    snakes.Insert(0, snake);
                else
                    snakes.Add(snake);
                hasSnakeEaten.Add(false);
                foodCounts.Add(0);
            }

            double bestScore = double.MinValue;
            Direction bestMove = Direction.NO_MOVE;
            state.MAX_DEPTH = amountOfSnakes;
            double prevHit = _hit;
            double prevNoHit = _noHit;
            double prevGoodHit = _goodHit;
            while (true)
            {
                if (IsTimeoutThresholdReached || state.MAX_DEPTH > 50)
                    break;

                List<bool> hasSnakeEatenTemp = new(hasSnakeEaten);
                List<int> foodCountsTemp = new(foodCounts);

                (double score, Direction move) = MaxNWithAlphaBeta(state, snakes, state.MAX_DEPTH, hasSnakeEatenTemp, foodCountsTemp);

                if (_searchCutOff)
                    break;

                if (Util.IsDebug)
                    Debug.WriteLine(score + " " + move + " " + state.MAX_DEPTH);

                bestScore = score;
                bestMove = move;
                state.MAX_DEPTH += amountOfSnakes;
                prevHit = _hit;
                prevNoHit = _noHit;
                prevGoodHit = _goodHit;
            }

            if (Util.IsDebug)
            {
                double total = prevHit + prevNoHit + prevGoodHit;
                WriteDebugMessage($"No hit: {prevNoHit} + hit: {prevHit} + good hit: {prevGoodHit} = Total: {total} -- Hit procentage: {prevHit / total * 100} Good hit procentage: {prevGoodHit / total * 100} Total success procentage: {(prevHit + prevGoodHit) / total * 100}");
                WriteDebugMessage($"Depth searched to: {state.MAX_DEPTH}, a score of: {bestScore} with move: {bestMove}");
            }

            return bestMove;
        }

        private Direction MaxNParallelIterativeDeepening()
        {
            if (Util.IsDebug)
                Print();

            double prevHit = _hit;
            double prevNoHit = _noHit;
            double prevGoodHit = _goodHit;
            ConcurrentBag<(double score, Direction move, int depth)> scores = new();
            int amountOfSnakes = _game.Board.Snakes.Count;
            Parallel.For(0, 4, i =>
            {
                State StateClone = _state.DeepClone();
                StateClone.MAX_DEPTH = amountOfSnakes * (i + 1);
                List<Snake> snakes = new();
                List<bool> hasSnakeEaten = new();
                List<int> foodCounts = new();
                for (int j = 0; j < amountOfSnakes; j++)
                {
                    Snake snake = _game.Board.Snakes[j].Clone();
                    if (snake.Id == _game.You.Id)
                        snakes.Insert(0, snake);
                    else
                        snakes.Add(snake);
                    hasSnakeEaten.Add(false);
                    foodCounts.Add(0);
                }
                while (true)
                {
                    if (IsTimeoutThresholdReached || StateClone.MAX_DEPTH > 50)
                        break;

                    List<bool> hasSnakeEatenTemp = new(hasSnakeEaten);
                    List<int> foodCountsTemp = new(foodCounts);

                    /*
                     * https://www.chessprogramming.org/Lazy_SMP
                     * https://chess.stackexchange.com/questions/35257/chess-engine-using-lazysmp
                     * 
                     * https://stackoverflow.com/questions/70303310/why-does-increasing-the-hash-table-size-for-a-chess-engine-also-drastically-inc
                     */
                    (double score, Direction move) = MaxNWithAlphaBeta(StateClone, snakes, StateClone.MAX_DEPTH, hasSnakeEatenTemp, foodCountsTemp);

                    if (_searchCutOff)
                        break;

                    scores.Add((score, move, StateClone.MAX_DEPTH));
                    StateClone.MAX_DEPTH += amountOfSnakes;
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
                WriteDebugMessage($"Depth searched to: {depth}, a score of: {bestScore} with move: {bestMove}");
            }

            return bestMove;
        }

        //http://fierz.ch/strategy2.htm#searchenhance -- HashTables
        //http://people.csail.mit.edu/plaat/mtdf.html#abmem
        private readonly Dictionary<int, TranspositionValue> _transpositionTable = new(); //Swap to ConcurrentDictionary if using multiple threads and remember to change method below
        private (double score, Direction move) MaxNWithAlphaBeta(State state, List<Snake> snakes, int depth, List<bool> hasSnakesEaten, List<int> foodCounts, int index = 0, double alpha = double.MinValue, double beta = double.MaxValue)
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
            if (_transpositionTable.TryGetValue(state.Key, out TranspositionValue value))
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
                double evaluatedState = EvaluateState(state, snakes, foodCounts);
                return (evaluatedState, Direction.NO_MOVE);
            }

            bool isMaximizer = index == 0;
            if (isMaximizer) //Only evaluate if game is over when it's my turn because it takes snake count depths for a turn
            {
                (double score, bool isGameOver) = EvaluateIfGameIsOver(snakes, depth, state.MAX_DEPTH);
                if (isGameOver) return (score, Direction.NO_MOVE);
            }

            Direction bestMove = Direction.NO_MOVE;
            int moveIndex = 0;
            Snake currentSnake = snakes[index];
            bool hasCurrentSnakeEaten = hasSnakesEaten[index];
            double bestMoveScore = isMaximizer ? double.MinValue : double.MaxValue;
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

                if (_gameMode == GameMode.WRAPPED) //Maybe cleanup and call neighbours method
                {
                    Point temp = Util.WrapPointCoordinates(_game.Board.Height, _game.Board.Width, dx, dy);
                    dx = temp.X;
                    dy = temp.Y;
                }

                if (IsInBounds(dx, dy))
                {
                    //var key = state.Key;
                    //var clone = state.DeepCloneGrid();

                    //Store values for updating hash
                    Point oldHead = new() { X = currentSnake.Head.X, Y = currentSnake.Head.Y };
                    Point oldNeck = new() { X = currentSnake.Body[1].X, Y = currentSnake.Body[1].Y };
                    Point oldTail = new() { X = currentSnake.Body.Last().X, Y = currentSnake.Body.Last().Y };
                    //Change state of the game
                    GameObject destinationTile = state.Grid[dx][dy];
                    int prevHp = currentSnake.Health;
                    int prevLength = currentSnake.Length;
                    currentSnake.Health -= _hazardSpots.Count > 0 && _hazardSpots.Contains((dx, dy)) ? _game.Game.Ruleset.Settings.HazardDamagePerTurn : 1;
                    bool hasEatenFood = false;
                    int currentFoodCount = foodCounts[index];
                    if (IsFoodTile(state.Grid, dx, dy))
                    {
                        IEnumerable<Snake> others = snakes.Where(s => s.Id != currentSnake.Id && s.Head.X == dx && s.Head.Y == dy && s.Length > currentSnake.Length);
                        if (!others.Any())
                        {
                            hasEatenFood = true;
                            currentSnake.Length++;
                            currentSnake.Health = HeuristicConstants.MAX_HEALTH;
                            currentFoodCount++;
                        }
                    }
                    else if (hasSnakesEaten.Any(s => s)) //To handle case where other snake eats a food from the same tile before current snake gets to it
                    {
                        for (int j = 0; j < snakes.Count; j++)
                        {
                            Snake other = snakes[j];
                            if (j != index && hasSnakesEaten[j] && dx == other.Head.X && dy == other.Head.Y && currentSnake.Length + 1 >= other.Length)
                            {
                                hasEatenFood = true;
                                currentSnake.Length++;
                                currentSnake.Health = HeuristicConstants.MAX_HEALTH;
                                currentFoodCount++;
                                destinationTile = GameObject.FOOD;
                                break;
                            }
                        }
                    }
                    //Move the snake
                    state.MoveSnakeForward(currentSnake, dx, dy, hasCurrentSnakeEaten);
                    state.DrawSnakesToGrid(snakes);
                    //Store values for updating hash
                    Point newHead = new() { X = currentSnake.Head.X, Y = currentSnake.Head.Y };
                    Point newTail = new() { X = currentSnake.Body.Last().X, Y = currentSnake.Body.Last().Y };
                    state.Key = ZobristHash.Instance.UpdateKeyForward(state.Key, oldNeck, oldHead, oldTail, newHead, newTail, destinationTile);
                    //Update list of which snake has eaten and how big of a food count they have
                    List<bool> currentHasSnakesEaten = new();
                    List<int> currentFoodCounts = new();
                    for (int j = 0; j < snakes.Count; j++)
                    {
                        currentHasSnakesEaten.Add(j == index ? hasEatenFood : hasSnakesEaten[j]);
                        currentFoodCounts.Add(j == index ? currentFoodCount : foodCounts[j]);
                    }
                    //Execute minimax
                    (double score, Direction move) = MaxNWithAlphaBeta(state: state,
                                                                      snakes: snakes,
                                                                      depth: depth - 1,
                                                                      hasSnakesEaten: currentHasSnakesEaten,
                                                                      foodCounts: currentFoodCounts,
                                                                      index: index + 1 >= snakes.Count ? 0 : index + 1,
                                                                      alpha: alpha,
                                                                      beta: beta);
                    if (!IsTimeoutThresholdReached) //Only clear if timeout is not reached
                    {
                        //Move the snake back
                        state.MoveSnakeBackward(currentSnake, oldTail, hasCurrentSnakeEaten, destinationTile);
                        state.DrawSnakesToGrid(snakes);
                        //Revert changes made doing previous state
                        currentSnake.Health = prevHp;
                        currentSnake.Length = prevLength;
                        //Revert changes made to the key
                        state.Key = ZobristHash.Instance.UpdateKeyBackward(state.Key, oldNeck, oldHead, oldTail, newHead, newTail, destinationTile);

                        //Debug.Assert(state.IsGridSame(clone));
                        //Debug.Assert(state.Key == key);
                    }

                    if (isMaximizer)
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

            if (!_transpositionTable.ContainsKey(state.Key))
            {
                double lowerBound = double.MinValue, upperbound = double.MaxValue;
                if (bestMoveScore <= alpha) upperbound = bestMoveScore;
                if (bestMoveScore > alpha && bestMoveScore < beta)
                {
                    upperbound = bestMoveScore;
                    lowerBound = bestMoveScore;
                }
                if (bestMoveScore >= beta) lowerBound = bestMoveScore;
                _transpositionTable.Add(state.Key, new() { Move = bestMove, MoveIndex = moveIndex, Depth = depth, LowerBound = lowerBound, UpperBound = upperbound });
            }

            return (bestMoveScore, bestMove);
        }

        private (double score, Direction move) MinimaxWithAlphaBeta(State state, Snake me, Snake other, int depth, bool isMaximizingPlayer = true, double alpha = double.MinValue, double beta = double.MaxValue, bool hasMaximizerEaten = false, bool hasMinimizerEaten = false, int myFoodCount = 0, int otherFoodCount = 0)
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
            if (_transpositionTable.TryGetValue(state.Key, out TranspositionValue value))
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

            List<Snake> snakes = new() { me, other };
            if (depth == 0)
            {
                double evaluatedState = EvaluateState(state, snakes, new() { myFoodCount, otherFoodCount });
                return (evaluatedState, Direction.NO_MOVE);
            }

            if (isMaximizingPlayer) //Only evaluate if game is over when it's my turn because it takes two depths for a turn
            {
                (double score, bool isGameOver) = EvaluateIfGameIsOver(snakes, depth, state.MAX_DEPTH);
                if (isGameOver) return (score, Direction.NO_MOVE);
            }

            Direction bestMove = Direction.NO_MOVE;
            int moveIndex = 0;
            Snake currentSnake = isMaximizingPlayer ? me : other;
            bool hasCurrentSnakeEaten = isMaximizingPlayer ? hasMaximizerEaten : hasMinimizerEaten;
            double bestMoveScore = isMaximizingPlayer ? double.MinValue : double.MaxValue;
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
                    //int key = state.Key;
                    //var clone = state.DeepCloneGrid();
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
                    int currentFoodCount = isMaximizingPlayer ? myFoodCount : otherFoodCount;
                    if (isFoodTile)
                    {
                        Snake temp = !isMaximizingPlayer ? me : other;
                        if (dx != temp.Head.X || dy != temp.Head.Y || currentSnake.Length > temp.Length)
                        {
                            currentSnake.Length++;
                            currentSnake.Health = HeuristicConstants.MAX_HEALTH;
                            currentFoodCount++;
                        }
                    }
                    else if (!isMaximizingPlayer && hasMaximizerEaten && dx == me.Head.X && dy == me.Head.Y && currentSnake.Length + 1 >= me.Length) //To handle case where maximizer eats a food from the same tile before minimizer gets to it
                    {
                        currentSnake.Length++;
                        currentSnake.Health = HeuristicConstants.MAX_HEALTH;
                        currentFoodCount++;
                        isFoodTile = true;
                        destinationTile = GameObject.FOOD;
                    }
                    //Move the snake
                    state.MoveSnakeForward(currentSnake, x, y, hasCurrentSnakeEaten);
                    state.DrawSnakesToGrid(snakes);
                    //Store values for updating hash
                    Point newHead = new() { X = currentSnake.Head.X, Y = currentSnake.Head.Y };
                    Point newTail = new() { X = currentSnake.Body.Last().X, Y = currentSnake.Body.Last().Y };
                    state.Key = ZobristHash.Instance.UpdateKeyForward(state.Key, oldNeck, oldHead, oldTail, newHead, newTail, destinationTile);
                    //Execute minimax
                    (double score, Direction move) = MinimaxWithAlphaBeta(state: state,
                                                                me: isMaximizingPlayer ? currentSnake : me,
                                                                other: !isMaximizingPlayer ? currentSnake : other,
                                                                depth: depth - 1,
                                                                isMaximizingPlayer: !isMaximizingPlayer,
                                                                alpha: alpha,
                                                                beta: beta,
                                                                hasMaximizerEaten: isMaximizingPlayer ? isFoodTile : hasMaximizerEaten,
                                                                hasMinimizerEaten: !isMaximizingPlayer ? isFoodTile : hasMinimizerEaten,
                                                                myFoodCount: isMaximizingPlayer ? currentFoodCount : myFoodCount,
                                                                otherFoodCount: !isMaximizingPlayer ? currentFoodCount : otherFoodCount);
                    if (!IsTimeoutThresholdReached) //Only clear if timeout is not reached
                    {
                        //Move the snake back
                        state.MoveSnakeBackward(currentSnake, oldTail, hasCurrentSnakeEaten, destinationTile);
                        state.DrawSnakesToGrid(snakes);
                        //Revert changes made doing previous state
                        currentSnake.Health = prevHp;
                        currentSnake.Length = prevLength;
                        //Revert changes made to the key
                        state.Key = ZobristHash.Instance.UpdateKeyBackward(state.Key, oldNeck, oldHead, oldTail, newHead, newTail, destinationTile);
                        //Debug.Assert(state.IsGridSame(clone));
                        //Debug.Assert(state.Key == key);
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
            if (bestMoveScore > alpha && bestMoveScore < beta)
            {
                upperbound = bestMoveScore;
                lowerBound = bestMoveScore;
            }
            if (bestMoveScore >= beta) lowerBound = bestMoveScore;
            _transpositionTable.TryAdd(state.Key, new() { Move = bestMove, MoveIndex = moveIndex, Depth = depth, LowerBound = lowerBound, UpperBound = upperbound });

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
            List<Point> foods = new();
            for (int i = 0; i < _game.Board.Food.Count; i++)
            {
                Point food = _game.Board.Food[i];
                if (IsFoodTile(grid, food.X, food.Y))
                    foods.Add(new Point() { X = food.X, Y = food.Y });
            }
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
        private static bool IsMovingLeft(Snake snake) => snake.Head.Y + 1 == snake.Body[1].Y;
        private static bool IsMovingRight(Snake snake) => snake.Head.Y - 1 == snake.Body[1].Y;
        private static bool IsMovingUp(Snake snake) => snake.Head.X + 1 == snake.Body[1].X;
        private static bool IsMovingDown(Snake snake) => snake.Head.X - 1 == snake.Body[1].X;

        private double EvaluateState(State state, List<Snake> snakes, List<int> foodsCounts)
        {
            //----- Is game over -----
            (double score, bool isGameOver) evaluateIfGameIsOver = EvaluateIfGameIsOver(snakes, 0, state.MAX_DEPTH);
            if (evaluateIfGameIsOver.isGameOver)
                return evaluateIfGameIsOver.score;

            //Set variables
            int h = state.Grid.Length, w = state.Grid.First().Length;
            Snake me = snakes[0];
            (Snake other, int otherIndex) = FindClosestSnakeUsingManhattanDistanceToHead(snakes);
            int myFoodCount = foodsCounts[0];
            int otherFoodCount = foodsCounts[otherIndex];
            double score = 0d;
            Point myHead = me.Head;
            Point otherHead = other.Head;
            int myLength = me.Length;
            int otherLength = other.Length;
            int maxDistance = h + w;
            List<Point> availableFoods = GetFoodFromGrid(state.Grid);

            //----- Aggresion -----
            double aggresionScore = 0d;
            if (myLength >= otherLength + 2)
            {
                Point otherNeck = other.Body[1];
                Point otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y };
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

                        if (_gameMode == GameMode.WRAPPED)
                        {
                            Point temp = Util.WrapPointCoordinates(h, w, possibleMove1.X, possibleMove1.Y);
                            possibleMove1.X = temp.X;
                            possibleMove1.Y = temp.Y;
                            temp = Util.WrapPointCoordinates(h, w, possibleMove2.X, possibleMove2.Y);
                            possibleMove2.X = temp.X;
                            possibleMove2.Y = temp.Y;
                        }

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

                        if (_gameMode == GameMode.WRAPPED)
                        {
                            Point temp = Util.WrapPointCoordinates(h, w, possibleMove1.X, possibleMove1.Y);
                            possibleMove1.X = temp.X;
                            possibleMove1.Y = temp.Y;
                            temp = Util.WrapPointCoordinates(h, w, possibleMove2.X, possibleMove2.Y);
                            possibleMove2.X = temp.X;
                            possibleMove2.Y = temp.Y;
                        }

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
                    if (IsMovingDown(other))
                        otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                    else if (IsMovingUp(other))
                        otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                    else if (IsMovingRight(other))
                        otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                    else if (IsMovingLeft(other))
                        otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                }

                if (_gameMode == GameMode.WRAPPED)
                {
                    Point temp = Util.WrapPointCoordinates(h, w, otherSnakeMove.X, otherSnakeMove.Y);
                    otherSnakeMove.X = temp.X;
                    otherSnakeMove.Y = temp.Y;
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
                    Point otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y };
                    //I'm one tile ahead of the other snake and therefor I can cornor trap him
                    if (IsOnRightEdge(otherHead))
                    {
                        if (IsAheadOnRightEdgeGoingUp(myHead, otherHead, myNeck, me))
                            otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                        else if (IsAheadOnRightEdgeGoingDown(myHead, otherHead, myNeck, me))
                            otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                        else //Try to predict snake move
                        {
                            if (IsMovingDown(other))
                                otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                            else if (IsMovingUp(other))
                                otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                            else if (IsMovingRight(other))
                            {
                                if (Util.RollThiftyThiftyChance())
                                    otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                                else
                                    otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                            }
                        }
                    }
                    else if (IsOnLeftEdge(otherHead))
                    {
                        if (IsAheadOnLeftEdgeGoingUp(myHead, otherHead, myNeck, me))
                            otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                        else if (IsAheadOnLeftEdgeGoingDown(myHead, otherHead, myNeck, me))
                            otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                        else //Try to predict snake move
                        {
                            if (IsMovingDown(other))
                                otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                            else if (IsMovingUp(other))
                                otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                            else if (IsMovingLeft(other))
                            {
                                if (Util.RollThiftyThiftyChance())
                                    otherSnakeMove = new() { X = otherHead.X + 1, Y = otherHead.Y };
                                else
                                    otherSnakeMove = new() { X = otherHead.X - 1, Y = otherHead.Y };
                            }
                        }
                    }
                    else if (IsOnTopEdge(otherHead))
                    {
                        if (IsAheadOnTopEdgeGoingLeft(myHead, otherHead, myNeck, me))
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                        else if (IsAheadOnTopEdgeGoingRight(myHead, otherHead, myNeck, me))
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                        else //Try to predict snake move
                        {
                            if (IsMovingRight(other))
                                otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                            else if (IsMovingLeft(other))
                                otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                            else if (IsMovingUp(other))
                            {
                                if (Util.RollThiftyThiftyChance())
                                    otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                                else
                                    otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                            }
                        }
                    }
                    else if (IsOnBottomEdge(otherHead))
                    {
                        if (IsAheadOnBottomEdgeGoingLeft(myHead, otherHead, myNeck, me))
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                        else if (IsAheadOnBottomEdgeGoingRight(myHead, otherHead, myNeck, me))
                            otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                        else //Try to predict snake move
                        {
                            if (IsMovingRight(other))
                                otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                            else if (IsMovingLeft(other))
                                otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                            else if (IsMovingDown(other))
                            {
                                if (Util.RollThiftyThiftyChance())
                                    otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y + 1 };
                                else
                                    otherSnakeMove = new() { X = otherHead.X, Y = otherHead.Y - 1 };
                            }
                        }
                    }

                    if (_gameMode == GameMode.WRAPPED)
                    {
                        Point temp = Util.WrapPointCoordinates(h, w, otherSnakeMove.X, otherSnakeMove.Y);
                        otherSnakeMove.X = temp.X;
                        otherSnakeMove.Y = temp.Y;
                    }

                    if (!IsInBounds(otherSnakeMove.X, otherSnakeMove.Y)) //Not a valid move
                        otherSnakeMove = RandomSafeMove(state.Grid, other);

                    int manhattenDistanceToOtherSnake = Util.ManhattenDistance(myHead.X, myHead.Y, otherSnakeMove.X, otherSnakeMove.Y);
                    int distanceToOtherSnake = Math.Abs(maxDistance - manhattenDistanceToOtherSnake);
                    aggresionScore = distanceToOtherSnake * HeuristicConstants.AGGRESSION_VALUE;
                }
            }
            score += aggresionScore;

            //----- Voronoi & Food -----
            (int score, int ownedFoodDepth)[] voronoi = VoronoiAlgorithm.VoronoiStateHeuristic(state.Grid, _gameMode, me, other);
            (int score, int ownedFoodDepth) myVoronoi = voronoi[0];
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
                        //Fallback to manhatten distance
                        Point myClosestFood = FindClosestFoodUsingManhattenDistance(availableFoods, myHead);
                        ownedFoodDistance = Util.ManhattenDistance(myHead.X, myHead.Y, myClosestFood.X, myClosestFood.Y);
                    }
                }

                if (me.Health - 3 < ownedFoodDistance) //Minus 3 to give 3 ekstra turns to reach food
                    return -10000d;

                double rope = me.Health - ownedFoodDistance;
                myFoodScore += HeuristicConstants.MY_FOOD_VALUE * Math.Atan(rope / HeuristicConstants.ATAN_VALUE);

                if (myLength < otherLength + 2)
                {
                    if (myFoodCount > 0)
                        myFoodScore += HeuristicConstants.MY_FOOD_VALUE * myFoodCount;
                    else
                        myFoodScore += Math.Pow((maxDistance - ownedFoodDistance) / 4, 2);
                }
            }
            score += myFoodScore;

            (int score, int ownedFoodDepth) otherVoronoi = voronoi[1];
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
                        //Fallback to manhatten distance
                        Point otherClosestFood = FindClosestFoodUsingManhattenDistance(availableFoods, otherHead);
                        ownedFoodDistance = Util.ManhattenDistance(otherHead.X, otherHead.Y, otherClosestFood.X, otherClosestFood.Y);
                    }
                }

                if (other.Health - 3 < ownedFoodDistance) //Minus 3 to give 3 ekstra turns to reach food
                    return 10000d;

                double rope = other.Health - ownedFoodDistance;
                theirFoodScore -= HeuristicConstants.OTHER_FOOD_VALUE * Math.Atan(rope / HeuristicConstants.ATAN_VALUE);

                if (otherLength < myLength + 2)
                {
                    if (otherFoodCount > 0)
                        theirFoodScore -= HeuristicConstants.OTHER_FOOD_VALUE * otherFoodCount;
                    else
                        theirFoodScore += Math.Pow((maxDistance - ownedFoodDistance) / 4, 2);
                }
            }
            score += theirFoodScore;

            //----- Flood fill -----
            double myFloodFillScore = CalculateFloodfillScore(state.Grid, me) * HeuristicConstants.MY_FLOODFILL_VALUE;
            double otherFloodFillScore = -1d * CalculateFloodfillScore(state.Grid, other) * HeuristicConstants.OTHER_FLOODFILL_VALUE;
            double floodFillScore = myFloodFillScore + otherFloodFillScore;
            score += floodFillScore;

            if (_gameMode != GameMode.ROYALE || _gameMode == GameMode.ROYALE && _hazardSpots.Count == 0)
            {
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
            }

            if (_gameMode != GameMode.WRAPPED || _gameMode == GameMode.WRAPPED && _hazardSpots.Count == 0)
            {
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
            }

            if (_hazardSpots.Count > 0)
            {
                //----- HAZARD -----
                double hazardScore = 0d;
                //Bad for me if I'm in the hazard
                if (_hazardSpots.Contains((myHead.X, myHead.Y)))
                    hazardScore += Math.Pow((HeuristicConstants.MAX_SNAKE_LENGTH - myLength) / 4, 2) * HeuristicConstants.MY_HAZARD_SCORE;

                //Good for me if other is in the hazard
                if (_hazardSpots.Contains((otherHead.X, otherHead.Y)))
                    hazardScore += Math.Pow((HeuristicConstants.MAX_SNAKE_LENGTH - otherLength) / 4, 2) * HeuristicConstants.OTHER_HAZARD_SCORE;
                score += hazardScore;
            }

            return score;
        }

        private static bool IsOnAnyLineSecondFromEdge(int height, int width, Point myHead) => myHead.X == 1 || myHead.X == height - 2 || myHead.Y == 1 || myHead.Y == width - 2;
        private static bool InnerCenter(Point head) => head.X >= 3 && head.X <= 7 && head.Y >= 3 && head.Y <= 7;
        private static bool OuterCenter(Point head) => head.X == 2 && head.Y >= 2 && head.Y <= 8 || //Upper center
                                                       head.X == 8 && head.Y >= 2 && head.Y <= 8 || //Lower center
                                                       head.Y == 2 && head.X >= 2 && head.X <= 8 || //Left center
                                                       head.Y == 8 && head.X >= 2 && head.X <= 8;   //Right center

        private (Snake other, int otherIndex) FindClosestSnakeUsingManhattanDistanceToHead(List<Snake> snakes)
        {
            Snake me = snakes[0];
            int otherIndex = 0, currentMin = int.MaxValue;
            Snake other = null;
            for (int i = 1; i < snakes.Count; i++) //Start at one so we skip ourselves
            {
                Snake snake = snakes[i];
                int min = Util.ManhattenDistance(snake.Head.X, snake.Head.Y, me.Head.X, me.Head.Y);
                if (currentMin > min)
                {
                    currentMin = min;
                    otherIndex = i;
                    other = snake;
                }
            }
            return (other, otherIndex);
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

        private double CalculateFloodfillScore(GameObject[][] grid, Snake snake)
        {
            double round = Math.Round(HeuristicConstants.SAFE_CAVERN_SIZE * snake.Length);
            int maxLength = (int)round;
            int cavernSize = BestAdjacentFloodFill(grid, snake.Head, maxLength);
            if (cavernSize >= maxLength) return 0d;
            double floodFillScore = (HeuristicConstants.MAX_FLOODFILL_SCORE - HeuristicConstants.MIN_FLOODFILL_SCORE) / Math.Sqrt(HeuristicConstants.SAFE_CAVERN_SIZE * HeuristicConstants.MAX_SNAKE_LENGTH) * Math.Sqrt(cavernSize) - HeuristicConstants.MAX_FLOODFILL_SCORE;
            return floodFillScore;
        }

        private Point RandomSafeMove(GameObject[][] grid, Snake snake)
        {
            List<(int x, int y)> safeTiles = Neighbours(grid, snake.Head.X, snake.Head.Y);
            if (safeTiles.Count == 0) return new() { X = Math.Abs(snake.Head.X - 1), Y = snake.Head.Y }; //Up or down if out of bounds
            int index = _rand.Next(0, safeTiles.Count);
            Point nextMove = new() { X = safeTiles[index].x, Y = safeTiles[index].y };
            return nextMove;
        }

        private (double score, bool isGameOver) EvaluateIfGameIsOver(List<Snake> snakes, int remainingDepth, int maxDepth)
        {
            Snake me = snakes[0];
            (Snake other, int otherIndex) = FindClosestSnakeUsingManhattanDistanceToHead(snakes);
            Point myHead = me.Head;
            Point otherHead = other.Head;
            bool mySnakeDead = false;
            bool mySnakeMaybeDead = false;
            bool otherSnakeDead = false;
            bool otherSnakeMaybeDead = false;
            bool headOnCollsion = false;

            if (!IsInBounds(myHead.X, myHead.Y))
                mySnakeMaybeDead = true;

            if (!IsInBounds(otherHead.X, otherHead.Y))
                otherSnakeMaybeDead = true;

            if (me.Health <= 0)
                mySnakeMaybeDead = true;

            if (other.Health <= 0)
                otherSnakeMaybeDead = true;

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
                for (int i = 0; i < snakes.Count; i++)
                {
                    Snake current = snakes[i];
                    for (int j = 0; j < current.Body.Count; j++)
                    {
                        Point body = current.Body[j];
                        if (body.X == myHead.X && body.Y == myHead.Y)
                            mySnakeHeadOnCount++;

                        if (body.X == otherHead.X && body.Y == otherHead.Y) //Maybe wrap this in a if where we only apply body hit when they are either in their own body or mine
                            otherSnakeHeadOnCount++;
                    }
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
                score = AdjustForFutureUncertainty(-1000, remainingDepth, maxDepth);
            else if (mySnakeMaybeDead && otherSnakeMaybeDead)
                score = AdjustForFutureUncertainty(-375, remainingDepth, maxDepth);
            else if (mySnakeMaybeDead)
                score = AdjustForFutureUncertainty(-500, remainingDepth, maxDepth);
            else if (otherSnakeMaybeDead)
                score = AdjustForFutureUncertainty(500, remainingDepth, maxDepth);
            else if (otherSnakeDead)
                score = AdjustForFutureUncertainty(1000, remainingDepth, maxDepth);
            else
                score = AdjustForFutureUncertainty(0, remainingDepth, maxDepth);

            //Node depth score -- https://levelup.gitconnected.com/improving-minimax-performance-fc82bc337dfd section 9
            score += remainingDepth;

            bool isGameOver = false;
            if (mySnakeDead || otherSnakeDead || mySnakeMaybeDead || otherSnakeMaybeDead)
                isGameOver = true;

            return (score, isGameOver);
        }

        private static double AdjustForFutureUncertainty(double score, int remainingDepth, int maxDepth)
        {
            int pow = maxDepth - remainingDepth - 2;
            double futureUncertainty = Math.Pow(HeuristicConstants.FUTURE_UNCERTAINTY_FACOTR, pow);
            return score * futureUncertainty;
        }
        #endregion

        #region Flood fill
        private Direction BestAdjacentFloodFill(Snake me)
        {
            List<(int x, int y, int freeTiles)> list = new();
            foreach (var (x, y) in Neighbours(_grid, me.Head.X, me.Head.Y))
            {
                int dx = x, dy = y;
                int score = FloodFillWithLimit(_grid, new() { X = dx, Y = dy }, me.Length);
                dx -= me.Head.X;
                dy -= me.Head.Y;
                list.Add((dx, dy, score));
            }
            if (list.Count > 0)
            {
                (int x, int y, int freeTiles) = list.OrderByDescending(v => v.freeTiles).First();
                if (_dirs.TryGetValue((x, y), out Direction move))
                    return move;
            }
            return _dir;
        }

        private int BestAdjacentFloodFill(GameObject[][] grid, Point head, int limit)
        {
            int bestScore = int.MinValue;
            List<(int x, int y)> neighbours = Neighbours(grid, head.X, head.Y);
            for (int i = 0; i < neighbours.Count; i++)
            {
                (int x, int y) = neighbours[i];
                int score = FloodFillWithLimit(grid, new() { X = x, Y = y }, limit);
                bestScore = Math.Max(bestScore, score);
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

                List<(int x, int y)> neighbours = Neighbours(grid, x, y, false);
                for (int i = 0; i < neighbours.Count; i++)
                {
                    (int x, int y) neighbour = neighbours[i];
                    if (!isVisited[neighbour.x, neighbour.y])
                    {
                        queue.Enqueue((neighbour.x, neighbour.y));
                        isVisited[neighbour.x, neighbour.y] = true;
                        count++;
                    }
                }
            }
            return count;
        }
        #endregion

        #region Choose food
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
        #endregion

        #region Helper functions
        private List<(int x, int y)> Neighbours(GameObject[][] grid, int x, int y, bool allowTail = true)
        {
            int h = grid.Length, w = grid.First().Length;
            List<(int x, int y)> neighbours = new();
            for (int i = 0; i < _moves.Length; i++)
            {
                (int x, int y) move = _moves[i];
                int dx = move.x + x, dy = move.y + y;

                if (_gameMode == GameMode.WRAPPED)
                {
                    Point temp = Util.WrapPointCoordinates(h, w, dx, dy);
                    dx = temp.X;
                    dy = temp.Y;
                }

                if (allowTail && IsMoveableTileWithTail(grid, dx, dy) || !allowTail && IsMoveableTile(grid, dx, dy))
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

        private bool IsMoveableTileWithHeadAndTail(GameObject[][] grid, int x, int y)
        {
            return IsFreeTile(grid, x, y) || IsFoodTile(grid, x, y) || IsHeadTile(grid, x, y) || IsTailTile(grid, x, y);
        }

        private bool IsHeadTile(GameObject[][] grid, int x, int y)
        {
            return IsInBounds(x, y) && grid[x][y] == GameObject.HEAD;
        }

        private bool IsTailTile(GameObject[][] grid, int x, int y)
        {
            return IsInBounds(x, y) && grid[x][y] == GameObject.TAIL;
        }

        private bool IsMoveableTileWithTail(GameObject[][] grid, int x, int y)
        {
            return IsFreeTile(grid, x, y) || IsFoodTile(grid, x, y) || IsTailTile(grid, x, y);
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

        private GameObject[][] GenerateGrid()
        {
            GameObject[][] grid = new GameObject[_game.Board.Height][];
            //Init grid
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = new GameObject[_game.Board.Width];
                for (int j = 0; j < grid[i].Length; j++)
                    grid[i][j] = GameObject.FLOOR;
            }
            //Setup food on the grid
            for (int i = 0; i < _game.Board.Food.Count; i++)
            {
                Point food = _game.Board.Food[i];
                grid[food.X][food.Y] = GameObject.FOOD;
            }
            //Setup snakes on the grid
            for (int i = 0; i < _game.Board.Snakes.Count; i++)
            {
                Snake snake = _game.Board.Snakes[i];
                for (int j = 0; j < snake.Body.Count; j++)
                {
                    Point body = snake.Body[j];
                    grid[body.X][body.Y] = GameObject.BODY;
                }
                grid[snake.Head.X][snake.Head.Y] = GameObject.HEAD;
                Point tail = snake.Body.Last();
                grid[tail.X][tail.Y] = GameObject.TAIL;
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

        private void UpdateCoordinates(GameStatusDTO game)
        {
            int h = game.Board.Height - 1, temp;
            for (int i = 0; i < game.Board.Food.Count; i++)
            {
                //Update food
                Point food = game.Board.Food[i];
                food.Y = h - food.Y;
                temp = food.X;
                food.X = food.Y;
                food.Y = temp;
            }

            for (int i = 0; i < game.Board.Snakes.Count; i++)
            {
                //Update snake head
                Snake snake = game.Board.Snakes[i];
                snake.Head.Y = h - snake.Head.Y;
                temp = snake.Head.X;
                snake.Head.X = snake.Head.Y;
                snake.Head.Y = temp;
                for (int j = 0; j < snake.Body.Count; j++)
                {
                    //Update snake body
                    Point body = snake.Body[j];
                    body.Y = h - body.Y;
                    temp = body.X;
                    body.X = body.Y;
                    body.Y = temp;
                    if (snake.Id == game.You.Id)
                    {
                        //Update my body
                        Point youBody = game.You.Body[j];
                        youBody.Y = h - youBody.Y;
                        temp = youBody.X;
                        youBody.X = youBody.Y;
                        youBody.Y = temp;
                    }
                }
            }
            //Update my head
            game.You.Head.Y = h - game.You.Head.Y;
            temp = game.You.Head.X;
            game.You.Head.X = game.You.Head.Y;
            game.You.Head.Y = temp;

            if (_gameMode == GameMode.WRAPPED && game.Board.Hazards.Count > 0)
            {
                //Update hazard spots
                for (int i = 0; i < game.Board.Hazards.Count; i++)
                {
                    Point current = game.Board.Hazards[i];
                    current.Y = h - current.Y;
                    temp = current.X;
                    current.X = current.Y;
                    current.Y = temp;
                    _hazardSpots.Add((current.X, current.Y));
                }
            }
        }
        #endregion
    }
}
