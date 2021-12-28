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

        public State(GameObject[][] grid)
        {
            Grid = grid;
        }

        public void UpdateSnakesToGrid(Snake[] snakes)
        {
            for (int i = 0; i < snakes.Length; i++)
            {
                Snake snake = snakes[i];
                int n = snake.Body.Count;
                if (n >= 4)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        Point firstBody = snake.Body[j];
                        Grid[firstBody.X][firstBody.Y] = GameObject.BODY;
                        Point LastBody = snake.Body[n - 1 - j];
                        Grid[LastBody.X][LastBody.Y] = GameObject.BODY;
                    }
                }
                else
                {
                    for (int j = 0; j < n; j++)
                    {
                        Point body = snake.Body[j];
                        Grid[body.X][body.Y] = GameObject.BODY;
                    }
                }
                Grid[snake.Head.X][snake.Head.Y] = GameObject.HEAD;
            }
        }

        public void MoveSnakeForward(Snake current, int x, int y, bool isFoodTile)
        {
            Point newHead = new() { X = current.Head.X + x, Y = current.Head.Y + y };            
            Grid[current.Head.X][current.Head.Y] = GameObject.BODY;
            current.Body.Insert(0, new() { X = newHead.X, Y = newHead.Y });
            current.Head = new() { X = newHead.X, Y = newHead.Y };
            if (!isFoodTile)
            {
                Point tail = current.Body.Last();
                Grid[tail.X][tail.Y] = GameObject.FLOOR;
                current.Body.RemoveAt(current.Body.Count - 1);
            }
            Grid[newHead.X][newHead.Y] = GameObject.HEAD;
        }

        public void MoveSnakeBackward(Snake current, Point tail, bool isFoodTile, GameObject newHeadTile)
        {
            Grid[current.Head.X][current.Head.Y] = newHeadTile;
            current.Body.RemoveAt(0);
            current.Head = new() { X = current.Body.First().X, Y = current.Body.First().Y };
            if (isFoodTile)
            {
                Point last = current.Body.Last();
                Grid[last.X][last.Y] = GameObject.FLOOR;
                current.Body.RemoveAt(current.Body.Count - 1);
            }
            current.Body.Add(new() { X = tail.X, Y = tail.Y });
            Grid[tail.X][tail.Y] = GameObject.BODY;
            Grid[current.Head.X][current.Head.Y] = GameObject.HEAD;
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
            return new State(Grid)
            {
                Key = Key
            };
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
            return new State(clone)
            {
                Key = Key
            };
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
