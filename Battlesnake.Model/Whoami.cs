using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Battlesnake.Model
{
    public class Whoami
    {
        public string ApiVersion { get; set; } = "1";
        public string Author { get; set; } = "Bwuk";
        public string Color { get; private set; } = "#33cc33";
        public string Head { get; private set; } = "workout";
        public string Tail { get; private set; } = "weight";
        public string Version { get; private set; } = "0.0.1Beta";

        public Whoami(string colour, string version)
        {
            Color = colour;
            Version = version;
        }

        public Whoami(string head, string tail, string colour, string version)
        {
            Color = colour;
            Version = version;
            Head = head;
            Tail = tail;
        }
    }
}
