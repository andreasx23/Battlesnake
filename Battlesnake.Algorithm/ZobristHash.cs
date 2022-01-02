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
        private static readonly object _lock = new();
        private static ZobristHash _instance = null;
        private readonly Random _rand;
        private readonly int[][] _zobristNumbers;
        private readonly int _height;

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

        public static ZobristHash Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                else
                    throw new Exception($"You have to initialize an instance of {nameof(ZobristHash)}!");
            }
        }

        public static void InitZobirstHash(int height, int width)
        {
            if (_instance == null)
            {
                lock (_lock) //Only take a lock to create a new instance if non has been created
                {
                    if (_instance == null)
                    {
                        int seed = Guid.NewGuid().GetHashCode();
                        _instance = new ZobristHash(height, width, 4, seed);
                    }
                }
            }
        }

        //https://en.wikipedia.org/wiki/Zobrist_hashing
        //https://www.chessprogramming.org/Zobrist_Hashing        
        public int ConvertGridToHash(GameObject[][] grid)
        {
            int hash = 0;
            for (int i = 0; i < grid.Length; i++)
                for (int j = 0; j < grid[i].Length; j++)
                    hash ^= _zobristNumbers[(i * grid.Length) + j][Util.ConvertGameObjectToInt(grid[i][j])];
            return hash;
        }

        public int UpdateKeyForward(int hash, Point oldNeck, Point oldHead, Point oldTail, Point newHead, Point newTail, GameObject destinationTile)
        {
            int newHeadIndex = (newHead.X * _height) + newHead.X;
            hash ^= _zobristNumbers[newHeadIndex][Util.ConvertGameObjectToInt(destinationTile)]; //UNDO DESTINATION TILE
            int myOldHeadIndex = (oldHead.X * _height) + oldHead.X;
            hash ^= _zobristNumbers[myOldHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //UNDO OLD HEAD
            hash ^= _zobristNumbers[newHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //ADD NEW HEAD
            int myOldNeckIndex = (oldNeck.X * _height) + oldNeck.X;
            hash ^= _zobristNumbers[myOldNeckIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //UNDO OLD NECK
            hash ^= _zobristNumbers[myOldHeadIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD NEW NECK
            int myOldTailIndex = (oldTail.X * _height) + oldTail.X;
            hash ^= _zobristNumbers[myOldTailIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //UNDO OLD TAIL
            int myNewTailIndex = (newTail.X * _height) + newTail.X;
            hash ^= _zobristNumbers[myNewTailIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD OLD TAIL
            return hash;
        }

        public int UpdateKeyBackward(int hash, Point oldNeck, Point oldHead, Point oldTail, Point newHead, Point newTail, GameObject destinationTile)
        {
            int myOldTailIndex = (oldTail.X * _height) + oldTail.X;
            int myNewTailIndex = (newTail.X * _height) + newTail.X;
            hash ^= _zobristNumbers[myNewTailIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //UNDO NEW TAIL
            hash ^= _zobristNumbers[myOldTailIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD OLD TAIL
            int myOldNeckIndex = (oldNeck.X * _height) + oldNeck.X;
            int myOldHeadIndex = (oldHead.X * _height) + oldHead.X;
            hash ^= _zobristNumbers[myOldHeadIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //UNDO NEW NECK
            hash ^= _zobristNumbers[myOldNeckIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD OLD NECK
            int newHeadIndex = (newHead.X * _height) + newHead.X;
            hash ^= _zobristNumbers[newHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //UNDO NEW HEAD
            hash ^= _zobristNumbers[myOldHeadIndex][Util.ConvertGameObjectToInt(GameObject.HEAD)]; //ADD OLD HEAD
            hash ^= _zobristNumbers[newHeadIndex][Util.ConvertGameObjectToInt(destinationTile)]; //ADD DESTINATION TILE
            return hash;
        }
    }
}
