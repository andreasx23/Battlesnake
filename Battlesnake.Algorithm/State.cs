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
        public ZobristHash Hash { get; private set; }
        public int Key { get; private set; }

        private bool _allowTT = false;
        public State(GameObject[][] grid)
        {
            Grid = grid;
            Hash = new ZobristHash(grid.Length, grid.First().Length, 4);
            Key = Hash.ConvertGridToHash(grid);
        }

        public State(GameObject[][] grid, bool allowTT)
        {
            Grid = grid;
            _allowTT = allowTT;
            if (allowTT)
            {
                Hash = new ZobristHash(grid.Length, grid.First().Length, 4);
                Key = Hash.ConvertGridToHash(grid);
            }
        }

        public State(GameObject[][] grid, ZobristHash hash)
        {
            Grid = grid;
            Hash = hash;
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
            //Store postions for hash update
            Point oldHead = new() { X = snake.Head.X, Y = snake.Head.Y };
            Point oldTail = new() { X = snake.Body.Last().X, Y = snake.Body.Last().Y };
            //Move head + body of current snake forwards
            Point newHead = new() { X = snake.Body[0].X + x, Y = snake.Body[0].Y + y };
            GameObject destinationTile = Grid[newHead.X][newHead.Y];

            snake.Body.Insert(0, new() { X = newHead.X, Y = newHead.Y });
            snake.Head = new() { X = newHead.X, Y = newHead.Y };
            if (!isFoodTile) snake.Body.RemoveAt(snake.Body.Count - 1);

            //Key = Hash.UpdateHashForward(Key, oldHead, snake.Head, oldTail, destinationTile);
        }

        public void ShiftBodyBackwards(GameObject lastTile, Snake snake, Point tail, bool isFoodTile)
        {
            Point oldHead = new() { X = snake.Head.X, Y = snake.Head.Y };
            Point oldTail = new() { X = snake.Body.Last().X, Y = snake.Body.Last().Y };

            Grid[snake.Head.X][snake.Head.Y] = lastTile; //Update correct tile from previous move
            //Move head + body of current snake backwards
            snake.Body.RemoveAt(0);
            Point newHead = new() { X = snake.Body[0].X, Y = snake.Body[0].Y };
            snake.Head = new() { X = newHead.X, Y = newHead.Y };
            if (isFoodTile) snake.Body.RemoveAt(snake.Body.Count - 1);
            snake.Body.Add(new() { X = tail.X, Y = tail.Y });

            //Key = Hash.UpdateHashBackwards(Key, oldHead, snake.Head, oldTail, tail, lastTile);
        }

        public void UpdateKeyFromGrid()
        {
            Key = Hash.ConvertGridToHash(Grid);
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
            return new State(Grid, Hash)
            {
                Key = Key
            };
        }

        public State DeepClone()
        {
            int h = Grid.Length, w = Grid.First().Length;
            GameObject[][] clone = new GameObject[h][];
            for (int i = 0; i < h; i++)
            {
                clone[i] = new GameObject[w];
                for (int j = 0; j < w; j++)
                    clone[i][j] = Grid[i][j];
            }

            if (_allowTT)
            {
                return new State(clone, Hash)
                {
                    Key = Key
                };
            }
            else
            {
                return new State(clone, false)
                {
                    Key = Key
                };
            }
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
