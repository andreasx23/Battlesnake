using System;
using System.Diagnostics;

namespace Battlesnake.Utility
{
    public class Util
    {
        public static bool IsDebug => Debugger.IsAttached;

        public static string LogPrefix(string id)
        {
            return $"[{id}]";
        }
    }
}
