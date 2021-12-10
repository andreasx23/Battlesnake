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
        private static int[,] Dijkstra(GameObject[][] grid, Snake snake)
        {
            List<(int x, int y)> moves = new()
            {
                (0, -1), //Left
                (1, 0), //Down
                (0, 1), //Right
                (-1, 0) //Up
            };

            int h = grid.Length, w = grid.First().Length;
            int[,] dists = new int[h, w];
            bool[,] isVisisted = new bool[h, w];
            foreach (var b in snake.Body)
                dists[b.X, b.Y] = 1;

            Queue<(int x, int y)> queue = new();
            foreach (var (x, y) in moves)
            {
                int dx = x + snake.Head.X, dy = y + snake.Head.Y, steps = 1;
                if (IsValid(grid, dx, dy))
                {
                    isVisisted[dx, dy] = true;
                    dists[dx, dy] = steps;
                    queue.Enqueue((dx, dy));
                }
            }

            while (queue.Any())
            {
                (int x, int y) = queue.Dequeue();
                int steps = dists[x, y] + 1;
                foreach (var item in moves)
                {
                    int dx = item.x + x, dy = item.y + y;                    
                    if (IsValid(grid, dx, dy) && !isVisisted[dx, dy])
                    {
                        isVisisted[dx, dy] = true;
                        dists[dx, dy] = steps;
                        queue.Enqueue((dx, dy));
                    }
                }
            }
            return dists;
        }

        public static double Compute(GameObject[][] grid, Snake me, Snake other)
        {
            //Stopwatch watch = Stopwatch.StartNew();
            int h = grid.Length, w = grid.First().Length;
            int[,] myCosts = Dijkstra(grid, me), otherCosts = Dijkstra(grid, other);
            int myTiles = 0, otherTiles = 0;
            double maxCost = h * HeuristicConstants.MAX_VORONOI_COST_VALUE;
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    if (myCosts[i, j] < otherCosts[i, j] && myCosts[i, j] <= maxCost)
                        myTiles++;
                    if (otherCosts[i, j] < myCosts[i, j] && otherCosts[i, j] <= maxCost)
                        otherTiles++;
                }
            }
            double cost = (myTiles - otherTiles) / HeuristicConstants.VORONOI_VALUE;
            //if (Util.IsDebug) Debug.WriteLine($"Voronoi took: {watch.Elapsed} to run");
            return cost;
        }

        private static bool IsValid(GameObject[][] grid, int x, int y)
        {
            int h = grid.Length, w = grid.First().Length;
            return x >= 0 && x < h && y >= 0 && y < w && grid[x][y] != GameObject.BODY && grid[x][y] != GameObject.HEAD;
        }

        private static void Print(GameObject[][] grid, int[,] costs)
        {
            for (int i = 0; i < grid.Length * 3 + 1; i++)
                Debug.Write("#");

            Debug.WriteLine("");
            for (int i = 0; i < grid.Length; i++)
            {
                Debug.Write("#");
                string toPrint = string.Empty;
                for (int j = 0; j < grid[i].Length; j++)
                {
                    string temp = costs[i, j].ToString().PadLeft(2);
                    toPrint += temp + " ";
                }
                Debug.Write(toPrint.TrimEnd() + "#");
                Debug.WriteLine("");
            }

            for (int i = 0; i < grid.Length * 3 + 1; i++)
                Debug.Write("#");
            Debug.WriteLine("");
        }
    }
}
