using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Model
{
    public class Board
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public List<Point> Food { get; set; } = new List<Point>();
        public List<Point> Hazards { get; set; } = new List<Point>(); //Only some gamemodes have this
        public List<Snake> Snakes { get; set; } = new List<Snake>();
    }
}
