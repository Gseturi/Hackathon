using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator.Animations
{
    internal static class Animations
    {
        public static async Task ShowSpinnerAsync(string message, Task waitForTask)
        {
            var spinner = new[] { '|', '/', '-', '\\' };
            Console.Write(message + " ");
            int i = 0;

            while (!waitForTask.IsCompleted)
            {
                Console.Write(spinner[i % spinner.Length]);
                await Task.Delay(100);

                // Safely move cursor back
                if (Console.IsOutputRedirected || Console.CursorLeft <= 0)
                {
                    Console.Write("\b"); // just use backspace char
                }
                else
                {
                    if (Console.CursorLeft > 0)
                    {
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                }

                i++;
            }

            Console.WriteLine(" Done!");
        }

    }
}
