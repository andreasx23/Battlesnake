using Battlesnake.Enum;
using Battlesnake.Model;
using Battlesnake.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Algorithm
{
    public class VoronoiAlgorithm
    {
        //https://play.battlesnake.com/g/24b90861-5484-4ec9-9301-f8d03bf73bd8/
        //https://play.battlesnake.com/g/714ac5a2-4bbb-4a4a-a42e-93cce974966f/
        //https://github.com/nbw/battlesnake_crystal
        //https://www.a1k0n.net/2010/03/04/google-ai-postmortem.html
        //https://github.com/a1k0n/tronbot/blob/master/cpp/MyTronBot.cc
        //https://github.com/ellyn/tronbots/blob/master/heuristic.py -- THIS!

        private static readonly (int x, int y)[] _moves = new (int x, int y)[4]
        {
            (0, -1), //Left
            (1, 0), //Down
            (0, 1), //Right
            (-1, 0) //Up
        };
        private static GameObject[][] _grid;
        private const int FRIENDLY = 1;
        private const int ENEMY = 2;
        private const int ARTICULATION = 3;
        private const int INF = 10001;
        private static int[,] Dijkstra(int[,] state, int[,] dists, Snake snake)
        {
            int h = _grid.Length, w = _grid.First().Length;
            dists[snake.Head.X, snake.Head.Y] = 0;
            bool[,] isVisisted = new bool[h, w];
            Queue<(int x, int y)> queue = new();
            List<(int x, int y)> neighbours = Neighbours(snake.Head.X, snake.Head.Y);
            for (int i = 0; i < neighbours.Count; i++)
            {
                (int x, int y) = neighbours[i];
                dists[x, y] = 1;
                queue.Enqueue((x, y));
            }

            while (queue.Any())
            {
                (int x, int y) = queue.Dequeue();
                int steps = dists[x, y] + 1;
                neighbours = Neighbours(x, y);
                for (int i = 0; i < neighbours.Count; i++)
                {
                    (int x, int y) move = neighbours[i];
                    if (state[move.x, move.y] != 0)
                        continue;

                    dists[move.x, move.y] = Math.Min(dists[move.x, move.y], steps);

                    if (!isVisisted[move.x, move.y])
                    {
                        isVisisted[move.x, move.y] = true;
                        queue.Enqueue((move.x, move.y));
                    }
                }
            }
            return dists;
        }

        private static double ComputeVoroni(int[,] state, Snake me, Snake other)
        {
            int h = _grid.Length, w = _grid.First().Length;
            int[,] myDists = new int[h, w], otherDists = new int[h, w];
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    myDists[i, j] = INF;
                    otherDists[i, j] = INF;
                }
            }
            myDists = Dijkstra(state, myDists, me);
            otherDists = Dijkstra(state, otherDists, other);
            int myTiles = 0, otherTiles = 0, maxCost = h + w;
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    if (myDists[i, j] < otherDists[i, j] && myDists[i, j] <= maxCost)
                        myTiles++;
                    if (otherDists[i, j] < myDists[i, j] && otherDists[i, j] <= maxCost)
                        otherTiles++;
                }
            }
            double score = myTiles - otherTiles;
            return score;
        }

        //https://github.com/aleksiy325/snek-two/blob/da589b945e347c5178f6cc0c8b190a28651cce50/src/common/game_state.cpp
        //https://github.com/aleksiy325/snek-two/blob/da589b945e347c5178f6cc0c8b190a28651cce50/src/strategies/minimax_snake.cpp
        private static (int score, int depth) GameStateVoronoi(Snake me, Snake other)
        {
            int myId = 0;
            int otherId = 1;
            int depth = 0;
            int foodDepth = -1;
            (int x, int y, int id) pairDepthMark = (-1, -1, -1);
            const int MARK = -1;
            Queue<(int x, int y, int id)> queue = new();
            Dictionary<(int x, int y), int> isVisited = new();
            int[] counts = new int[2];
            queue.Enqueue((me.Head.X, me.Head.Y, myId));
            isVisited.Add((me.Head.X, me.Head.Y), myId);
            queue.Enqueue((other.Head.X, other.Head.Y, otherId));
            isVisited.Add((other.Head.X, other.Head.Y), otherId);
            queue.Enqueue(pairDepthMark);
            while (queue.Any())
            {
                (int x, int y, int id) current = queue.Dequeue();
                if (current == pairDepthMark)
                {
                    depth++;
                    queue.Enqueue(pairDepthMark);
                    if (queue.Peek() == pairDepthMark)
                        break;
                }
                else
                {
                    foreach (var (x, y) in Neighbours(current.x, current.y))
                    {
                        if (isVisited.TryGetValue((x, y), out int id))
                        {
                            if (id != MARK && id != current.id)
                            {
                                counts[id]--;
                                isVisited[(x, y)] = MARK;
                            }
                        }
                        else
                        {
                            if (_grid[x][y] == GameObject.FOOD && current.id == myId && foodDepth == -1)
                                foodDepth = depth;
                            counts[current.id]++;
                            isVisited[(x, y)] = current.id;
                            queue.Enqueue((x, y, current.id));
                        }
                    }
                }
            }
            return (counts[myId], foodDepth);
        }

        public static (int score, int depth) VoronoiHeuristicNew(GameObject[][] grid, Snake me, Snake other)
        {
            _grid = grid;
            return GameStateVoronoi(me, other);
        }

        public static double VoronoiHeuristic(GameObject[][] grid, Snake me, Snake other)
        {
            Stopwatch watch = Stopwatch.StartNew();
            _grid = grid;
            int[,] state = GetState(me, other);
            double score = ComputeVoroni(state, me, other);
            //if (Util.IsDebug) Debug.WriteLine($"Voronoi took: {watch.Elapsed} to run");
            return score;
        }

        private static int[,] GetState(Snake me, Snake other)
        {
            int h = _grid.Length, w = _grid.First().Length;
            int[,] state = new int[h, w];
            for (int i = 0; i < me.Body.Count; i++)
            {
                Point body = me.Body[i];
                state[body.X, body.Y] = FRIENDLY;
            }
            for (int i = 0; i < other.Body.Count; i++)
            {
                Point body = other.Body[i];
                state[body.X, body.Y] = ENEMY;
            }
            return state;
        }

        private static int[,] HopcroftTarjan(int[,] state)
        {
            int h = _grid.Length, w = _grid.First().Length;
            int[,] parents = new int[h, w], lows = new int[h, w], depths = new int[h, w];
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    parents[i, j] = INF;
                    lows[i, j] = INF;
                    depths[i, j] = INF;
                }
            }
            parents[0, 0] = -1;
            bool[,] isVisited = new bool[h, w];
            RecHopcroftTarjan(state, 0, 0, 0, depths, parents, isVisited, lows);
            return state;
        }

        private static int IdMat(int row, int column) => row + column;

        private static void RecHopcroftTarjan(int[,] state, int row, int column, int depth, int[,] depths, int[,] parents, bool[,] isVisited, int[,] lows)
        {
            isVisited[row, column] = true;
            depths[row, column] = depth;
            lows[row, column] = depth;
            int children = 0;

            List<(int x, int y)> neighbours = Neighbours(row, column);
            for (int i = 0; i < neighbours.Count; i++)
            {
                (int x, int y) = neighbours[i];
                if (state[x, y] != 0 && state[x, y] != ARTICULATION)
                    continue;

                if (!isVisited[x, y])
                {
                    parents[x, y] = IdMat(row, column);
                    RecHopcroftTarjan(state, x, y, depth + 1, depths, parents, isVisited, lows);
                    children++;
                    if (lows[x, y] >= depths[row, column] && parents[row, column] != -1)
                        state[row, column] = ARTICULATION;
                    lows[row, column] = Math.Min(lows[row, column], lows[x, y]);
                }
                else if (IdMat(x, y) != parents[row, column])
                    lows[row, column] = Math.Min(lows[row, column], depths[x, y]);
            }

            if (parents[row, column] == -1 && children >= 2)
                state[row, column] = ARTICULATION;
        }

        public static double ChamberHeuristic(GameObject[][] grid, Snake me, Snake other)
        {
            Stopwatch watch = Stopwatch.StartNew();
            _grid = grid;
            int[,] state = GetState(me, other);
            state = HopcroftTarjan(state);
            double score = ComputeVoroni(state, me, other);
            //if (Util.IsDebug) Debug.WriteLine($"Chamber heuristic voronoi took: {watch.Elapsed} to run");
            return score;
        }

        #region Private helper methods
        private static List<(int x, int y)> Neighbours(int x, int y)
        {
            List<(int x, int y)> neighbours = new();
            for (int i = 0; i < _moves.Length; i++)
            {
                (int x, int y) move = _moves[i];
                int dx = move.x + x, dy = move.y + y;
                if (IsValid(dx, dy))
                    neighbours.Add((dx, dy));
            }
            return neighbours;
        }

        private static bool IsValid(int x, int y)
        {
            int h = _grid.Length, w = _grid.First().Length;
            return x >= 0 && x < h && y >= 0 && y < w && _grid[x][y] != GameObject.BODY && _grid[x][y] != GameObject.HEAD;
        }

        private static void Print(int[,] costs)
        {
            for (int i = 0; i < _grid.Length * 3 + 1; i++)
                Debug.Write("#");

            Debug.WriteLine("");
            for (int i = 0; i < _grid.Length; i++)
            {
                Debug.Write("#");
                string toPrint = string.Empty;
                for (int j = 0; j < _grid[i].Length; j++)
                {
                    string temp = costs[i, j].ToString().PadLeft(2);
                    toPrint += temp + " ";
                }
                Debug.Write(toPrint.TrimEnd() + "#");
                Debug.WriteLine("");
            }

            for (int i = 0; i < _grid.Length * 3 + 1; i++)
                Debug.Write("#");
            Debug.WriteLine("");
        }
        #endregion
    }
}
