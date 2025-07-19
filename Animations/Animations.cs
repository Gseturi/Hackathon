using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator.Animations
{
    internal class Animations
    {
        public static void ShowSpinner(string message, Task whendone)
        {
            var spinner = new[] { '|', '/', '-', '\\' };
            Console.Write(message + " ");
            int i = 0;
            while (!whendone.IsCompleted)
            {
                Console.Write(spinner[i % spinner.Length]);
                Thread.Sleep(100);
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                i++;
            }
            Console.WriteLine("Done!");
        }

    }
}
