using Battlesnake.Model;
using System;

namespace Battlesnake.DTOModel
{
    public class GameStatusDTO
    {
        public Game Game { get; set; } = new();
        public int Turn { get; set; } = 0;
        public Board Board { get; set; }
        public Snake You { get; set; }
    }
}