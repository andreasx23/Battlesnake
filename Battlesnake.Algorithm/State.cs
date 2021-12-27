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
        public int Key { get;  set; } = 0;

        public State(GameObject[][] grid)
        {
            Grid = grid;
        }

        public void ClearSnakesFromGrid(List<Snake> snakes)
        {
            //Clear snakes from board
            for (int i = 0; i < snakes.Count; i++)
            {
                Snake snake = snakes[i];
                for (int j = 0; j < snake.Body.Count; j++)
                {
                    Point body = snake.Body[j];
                    Grid[body.X][body.Y] = GameObject.FLOOR;
                }
            }
        }

        public void ApplySnakesToGrid(List<Snake> snakes)
        {
            //Add snakes to board
            for (int i = 0; i < snakes.Count; i++)
            {
                Snake snake = snakes[i];
                for (int j = 0; j < snake.Body.Count; j++)
                {
                    Point body = snake.Body[j];
                    Grid[body.X][body.Y] = GameObject.BODY;
                }
                Grid[snake.Head.X][snake.Head.Y] = GameObject.HEAD;
            }
        }

        public void ShiftBodyForward(Snake snake, int x, int y, bool isFoodTile)
        {
            //Move head + body of current snake forwards
            Point newHead = new() { X = snake.Body[0].X + x, Y = snake.Body[0].Y + y };
            snake.Body.Insert(0, new() { X = newHead.X, Y = newHead.Y });
            snake.Head = new() { X = newHead.X, Y = newHead.Y };
            if (!isFoodTile) snake.Body.RemoveAt(snake.Body.Count - 1);
        }

        public void ShiftBodyBackwards(GameObject lastTile, Snake snake, Point tail, bool isFoodTile)
        {
            //Move head + body of current snake backwards
            Grid[snake.Head.X][snake.Head.Y] = lastTile; //Update correct tile from previous move
            snake.Body.RemoveAt(0);
            Point newHead = new() { X = snake.Body[0].X, Y = snake.Body[0].Y };
            snake.Head = new() { X = newHead.X, Y = newHead.Y };
            if (isFoodTile) snake.Body.RemoveAt(snake.Body.Count - 1);
            snake.Body.Add(new() { X = tail.X, Y = tail.Y });
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
