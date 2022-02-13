using Battlesnake.Algorithm.Structs;
using Battlesnake.Enum;
using Battlesnake.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Battlesnake.Algorithm
{
    public class State
    {
        private const bool IS_LOCAL = false;
        public GameObject[][] Grid { get; private set; }
        public int Key { get; set; } = 0;
        public int MAX_DEPTH { get; set; } = 0;
        
        public State(GameObject[][] grid)
        {
            Grid = grid;
            Key = ZobristHash.Instance.ConvertGridToHash(grid);
        }

        private State(GameObject[][] grid, int key, int maxDepth)
        {
            Grid = grid;
            Key = key;
            MAX_DEPTH = maxDepth;
        }

        public void DrawSnakesToGrid(List<Snake> snakes)
        {
            for (int i = 0; i < snakes.Count; i++)
            {
                Snake snake = snakes[i];
                Grid[snake.Head.X][snake.Head.Y] = GameObject.HEAD;
                Grid[snake.Body[1].X][snake.Body[1].Y] = GameObject.BODY;
                Grid[snake.Body[^2].X][snake.Body[^2].Y] = GameObject.BODY;
                Grid[snake.Body.Last().X][snake.Body.Last().Y] = GameObject.TAIL;
            }
        }

        public void MoveSnakeForward(Snake current, int x, int y, bool isFoodTile)
        {
            PointStruct newHead = new() { X = x, Y = y };
            Grid[current.Head.X][current.Head.Y] = GameObject.BODY;
            current.Body.Insert(0, new() { X = newHead.X, Y = newHead.Y });
            current.Head = new() { X = newHead.X, Y = newHead.Y };
            if (!isFoodTile)
            {
                Grid[current.Body.Last().X][current.Body.Last().Y] = GameObject.FLOOR;
                current.Body.RemoveAt(current.Body.Count - 1);
                Grid[current.Body.Last().X][current.Body.Last().Y] = GameObject.TAIL;
            }
            Grid[newHead.X][newHead.Y] = GameObject.HEAD;
        }

        public void MoveSnakeBackward(Snake current, PointStruct tail, bool isFoodTile, GameObject destinationTile)
        {
            Grid[current.Head.X][current.Head.Y] = destinationTile;
            current.Body.RemoveAt(0);
            current.Head = new() { X = current.Body.First().X, Y = current.Body.First().Y };
            if (isFoodTile)
            {
                Grid[current.Body.Last().X][current.Body.Last().Y] = GameObject.FLOOR;
                current.Body.RemoveAt(current.Body.Count - 1);
            }
            current.Body.Add(new() { X = tail.X, Y = tail.Y });
            Grid[current.Head.X][current.Head.Y] = GameObject.HEAD;
            Grid[tail.X][tail.Y] = GameObject.TAIL;
        }

        public bool IsGridSame(GameObject[][] other)
        {
            for (int i = 0; i < Grid.Length; i++)
            {
                for (int j = 0; j < Grid[i].Length; j++)
                {
                    if (Grid[i][j] != other[i][j])
                        return false;
                }
            }
            return true;
        }

        public State ShallowClone()
        {
            return new State(Grid, Key, MAX_DEPTH);
        }

        public GameObject[][] DeepCloneGrid()
        {
            int h = Grid.Length, w = Grid.First().Length;
            GameObject[][] clone = new GameObject[h][];
            for (int i = 0; i < h; i++)
            {
                clone[i] = new GameObject[w];
                for (int j = 0; j < w; j++)
                    clone[i][j] = Grid[i][j];
            }
            return clone;
        }

        public State DeepClone()
        {
            GameObject[][] clone = DeepCloneGrid();
            return new State(clone, Key, MAX_DEPTH);
        }

        public void Print()
        {
            if (IS_LOCAL)
            {
                for (int i = 0; i < Grid.Length + 2; i++)
                    Console.Write("#");

                Console.WriteLine();
                for (int i = 0; i < Grid.Length; i++)
                {
                    Console.Write("#");
                    for (int j = 0; j < Grid[i].Length; j++)
                        Console.Write((char)Grid[i][j]);
                    Console.Write("#");
                    Console.WriteLine();
                }

                for (int i = 0; i < Grid.Length + 2; i++)
                    Console.Write("#");
                Console.WriteLine();
                Thread.Sleep(25);
            }
            else
            {
                Debug.WriteLine("");
                for (int i = 0; i < Grid.Length + 2; i++)
                    Debug.Write("#");

                Debug.WriteLine("");
                for (int i = 0; i < Grid.Length; i++)
                {
                    Debug.Write("#");
                    for (int j = 0; j < Grid[i].Length; j++)
                        Debug.Write((char)Grid[i][j]);
                    Debug.Write("#");
                    Debug.WriteLine("");
                }

                for (int i = 0; i < Grid.Length + 2; i++)
                    Debug.Write("#");
                Debug.WriteLine("");
            }
        }
    }
}
