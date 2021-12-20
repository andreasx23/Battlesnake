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
    public class ZobristHash
    {
        private readonly static int SEED = Guid.NewGuid().GetHashCode();
        private readonly Random _rand;
        private readonly int[][] _zobristNumbers;
        private readonly int _height;

        //TODO FIND HOW TO IMPLEMENT IT WITH VARIABLE HEIGHTS ETC
        public ZobristHash(int height, int width, int amountOfPieces)
        {
            _rand = new Random(SEED);
            _zobristNumbers = new int[height * width][];
            for (int i = 0; i < _zobristNumbers.Length; i++)
            {
                _zobristNumbers[i] = new int[amountOfPieces];
                for (int j = 0; j < _zobristNumbers[i].Length; j++)
                    _zobristNumbers[i][j] = _rand.Next(0, int.MaxValue);
            }
            _height = height;
        }

        public ZobristHash(int height, int width, int amountOfPieces, int seed)
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

        public int ConvertGridToHash(GameObject[][] grid)
        {
            int hash = 0;
            for (int i = 0; i < grid.Length; i++)
                for (int j = 0; j < grid[i].Length; j++)
                    hash ^= _zobristNumbers[(i * grid.Length) + j][Util.ConvertGameObjectToInt(grid[i][j])];
            return hash;
        }

        //https://en.wikipedia.org/wiki/Zobrist_hashing
        //https://www.chessprogramming.org/Zobrist_Hashing
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

            int tailIndex = (tail.X * _height) + tail.Y;
            hash ^= _zobristNumbers[tailIndex][Util.ConvertGameObjectToInt(GameObject.FLOOR)]; //UNDO FLOOR
            hash ^= _zobristNumbers[tailIndex][Util.ConvertGameObjectToInt(GameObject.BODY)]; //ADD TAIL
            return hash;
        }
    }
}
