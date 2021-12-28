using Battlesnake.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using AForge.Genetic;
using AForge.Neuro;
using AForge.Neuro.Learning;
using Battlesnake.Algorithm;
using Battlesnake.Algorithm.GeneticAlgorithmTest;
using System.Threading.Tasks;

namespace Battlesnake.Train
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var snek = GA.FindBestSnake();
            //Console.Clear();
            //Console.WriteLine("BEST VALUES");
            //Console.WriteLine(snek.FUTURE_UNCERTAINTY_FACOTR);
            //Console.WriteLine(snek.AGGRESSION_VALUE);
            //Console.WriteLine(snek.MY_FOOD_VALUE);
            //Console.WriteLine(snek.OTHER_FOOD_VALUE);
            //Console.WriteLine(snek.MY_FLOODFILL_VALUE);
            //Console.WriteLine(snek.OTHER_FLOODFILL_VALUE);
            //Console.WriteLine(snek.VORONOI_VALUE);
            //Console.WriteLine(snek.EDGE_VALUE_INNER);
            //Console.WriteLine(snek.EDGE_VALUE_OUTER);
            //Console.WriteLine(snek.CENTER_VALUE_INNER);
            //Console.WriteLine(snek.CENTER_VALUE_OUTER);

            double score = PlayGame();
            //Parallel.For(0, 10, i =>
            //{
            //    score += PlayGame();
            //});
            Console.WriteLine(score);
            Console.ReadLine();

            //AlogoRunLocal local = new(11, 11, 2);
            //local.Play();
        }

        //https://stackoverflow.com/questions/1169910/waiting-for-the-command-to-complete-in-c-sharp
        private static double PlayGame()
        {
            //ProcessStartInfo startInfo = new()
            //{
            //    CreateNoWindow = false,
            //    UseShellExecute = false,
            //    WindowStyle = ProcessWindowStyle.Normal,
            //    FileName = "cmd.exe",
            //    Arguments = @"C:\Users\Andreas\Desktop\rules-main\battlesnake.exe play -W 11 -H 11 --name me --url http://192.168.0.101:81/api/gamev2 --name other --url http://192.168.0.101:81/api/gametest"
            //};

            //// Start the process with the info we specified.
            //// Call WaitForExit and then the using statement will close.
            //using (Process exeProcess = Process.Start(startInfo))
            //{
            //    exeProcess.WaitForExit();
            //    Console.WriteLine("Output: " + exeProcess.StandardOutput);
            //}

            //return 0d;

            Process cmd = new();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            //cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine(@"C:\Users\Andreas\Desktop\rules-main\battlesnake.exe play -W 11 -H 11 --name me --url http://192.168.0.101:81/api/gamev2 --name other --url http://192.168.0.101:81/api/gametest");//--viewmap");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            string output = cmd.StandardOutput.ReadToEnd();
            double score = 0d;
            Console.Clear();
            Console.WriteLine("Output: " + output);
            if (output.Contains("me is the winner"))
                score = 1d;
            else if (output.Contains("It was a draw"))
                score = 0.5d;
            return score;
        }
    }
}
