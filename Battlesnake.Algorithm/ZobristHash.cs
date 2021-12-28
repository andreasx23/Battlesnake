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
    public sealed class ZobristHash
    {
        private static readonly object padlock = new();
        private static ZobristHash _instance = null;
        private readonly Random _rand;
        private readonly int[][] _zobristNumbers;
        private readonly int _height;

        //TODO FIND HOW TO IMPLEMENT IT WITH VARIABLE HEIGHTS ETC
        //private ZobristHash(int height, int width, int amountOfPieces)
        //{
        //    _rand = new Random(SEED);
        //    _zobristNumbers = new int[height * width][];
        //    for (int i = 0; i < _zobristNumbers.Length; i++)
        //    {
        //        _zobristNumbers[i] = new int[amountOfPieces];
        //        for (int j = 0; j < _zobristNumbers[i].Length; j++)
        //            _zobristNumbers[i][j] = _rand.Next(0, int.MaxValue);
        //    }
        //    _height = height;
        //}

        public static ZobristHash Instance
        {
            get
            {
                if (_instance != null) 
                    return _instance;
                else
                {
                    lock (padlock) //Only take a lock to create a new instance if non has been created
                    {
                        if (_instance == null)
                        {
                            int seed = Guid.NewGuid().GetHashCode();
                            _instance = new ZobristHash(11, 11, 4, seed);
                        }
                    }
                    return _instance;
                }
            }
        }

        private ZobristHash(int height, int width, int amountOfPieces, int seed)
        {
            _rand = new Random(seed);
            _zobristNumbers = new int[height * width][];
            for (int i = 0; i < _zobristNumbers.Length; i++)
            {
                _zobristNumbers[i] = new int[amountOfPieces];
                for (int j = 0; j < _zobristNumbers[i].Length; j++)
                    _zobristNumbers[i][j] = _rand.Next(0, int.MaxValue);
            }
            _height = height;
        }

        //https://en.wikipedia.org/wiki/Zobrist_hashing
        //https://www.chessprogramming.org/Zobrist_Hashing
        public int GenerateKey(Snake me, Snake other)
        {
            int hash = 0;
            int myHeadIndex = (me.Head.X * _height) + me.Head.X, myNeckIndex = (me.Body[1].X * _height) + me.Body[1].X;
            hash ^= _zobristNumbers[myHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //ADD HEAD
            hash ^= _zobristNumbers[myNeckIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD BODY
            int otherHeadIndex = (other.Head.X * _height) + other.Head.X, otherNeckIndex = (other.Body[1].X * _height) + other.Body[1].X;
            hash ^= _zobristNumbers[otherHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //ADD HEAD
            hash ^= _zobristNumbers[otherNeckIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD BODY
            return hash;
        }

        public int UpdateKeyForward(int hash, Point myOldNeck, Point myOldHead, Point myNewHead)
        {
            int myOldHeadIndex = (myOldHead.X * _height) + myOldHead.X;
            hash ^= _zobristNumbers[myOldHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //UNDO OLD HEAD
            int newHeadIndex = (myNewHead.X * _height) + myNewHead.X;
            hash ^= _zobristNumbers[newHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //ADD NEW HEAD
            int myOldNeckIndex = (myOldNeck.X * _height) + myOldNeck.X;
            hash ^= _zobristNumbers[myOldNeckIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //UNDO OLD NECK
            hash ^= _zobristNumbers[myOldHeadIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD NEW NECK
            return hash;
        }

        public int UpdateKeyBackward(int hash, Point myOldNeck, Point myOldHead, Point myNewHead)
        {
            int myOldNeckIndex = (myOldNeck.X * _height) + myOldNeck.X;
            int myOldHeadIndex = (myOldHead.X * _height) + myOldHead.X;
            hash ^= _zobristNumbers[myOldHeadIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //UNDO NEW NECK
            hash ^= _zobristNumbers[myOldNeckIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD OLD NECK
            int newHeadIndex = (myNewHead.X * _height) + myNewHead.X;
            hash ^= _zobristNumbers[newHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //UNDO NEW HEAD
            hash ^= _zobristNumbers[myOldHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //ADD OLD HEAD
            return hash;
        }

        public int ConvertGridToHash(GameObject[][] grid)
        {
            int hash = 0;
            for (int i = 0; i < grid.Length; i++)
                for (int j = 0; j < grid[i].Length; j++)
                    hash ^= _zobristNumbers[(i * grid.Length) + j][Util.ConvertGameObjectToInt(grid[i][j])];
            return hash;
        }

        public int UpdateHashForward(int hash, Point oldHead, Point newHead, Point oldTail, GameObject destinationTile)
        {
            int headIndex = (newHead.X * _height) + newHead.Y;
            hash ^= _zobristNumbers[headIndex][Util.ConvertGameObjectToInt(destinationTile)]; //UNDO DESTINATION TILE
            hash ^= _zobristNumbers[headIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //ADD HEAD

            headIndex = (oldHead.X * _height) + oldHead.Y;
            hash ^= _zobristNumbers[headIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //UNDO HEAD
            hash ^= _zobristNumbers[headIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD BODY

            if (destinationTile != GameObject.FOOD)
            {
                int tailIndex = (oldTail.X * _height) + oldTail.Y;
                hash ^= _zobristNumbers[tailIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //UNDO TAIL
                hash ^= _zobristNumbers[tailIndex][Util.ConvertGameObjectToInt(GameObject.FLOOR)]; //ADD FLOOR
            }
            return hash;
        }

        public int UpdateHashBackwards(int hash, Point oldHead, Point newHead, Point oldTail, Point tail, GameObject previousTile)
        {
            int headIndex = (oldHead.X * _height) + oldHead.Y;
            hash ^= _zobristNumbers[headIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //UNDO HEAD
            hash ^= _zobristNumbers[headIndex][Util.ConvertGameObjectToInt(previousTile)]; //ADD PREVIOUS TILE

            headIndex = (newHead.X * _height) + newHead.Y;
            hash ^= _zobristNumbers[headIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //UNDO BODY
            hash ^= _zobristNumbers[headIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //ADD HEAD

            if (previousTile == GameObject.FOOD)
            {
                int oldTailIndex = (oldTail.X * _height) + oldTail.Y;
                hash ^= _zobristNumbers[oldTailIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //UNDO TAIL
                hash ^= _zobristNumbers[oldTailIndex][Util.ConvertGameObjectToInt(GameObject.FLOOR)]; //ADD FLOOR
            }
            return hash;
        }
    }
}
