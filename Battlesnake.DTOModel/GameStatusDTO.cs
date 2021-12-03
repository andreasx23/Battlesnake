using Battlesnake.Model;
using System;

namespace Battlesnake.DTOModel
{
    public class GameStatusDTO
    {
        public Game Game { get; set; }
        public int Turn { get; set; }
        public Board Board { get; set; }
        public Snake You { get; set; }
    }
}