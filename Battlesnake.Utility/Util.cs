using Battlesnake.Enum;
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

        public static int ManhattenDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
        }

        public static int ConvertGameObjectToInt(GameObject obj)
        {
            return obj switch
            {
                GameObject.FLOOR => 0,
                GameObject.FOOD => 1,
                GameObject.HEAD => 2,
                GameObject.BODY => 3,
                _ => throw new Exception("Invalid piece"),
            };
        }
    }
}
