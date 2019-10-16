using System;

namespace CASCToolHost
{
    public static class Logger
    {
        public static void WriteLine(string line)
        {
            Console.WriteLine("[" + DateTime.Now.ToString() + "] " + line);
        }

        public static void WriteLine(string line, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("[" + DateTime.Now.ToString() + "] " + line);
            Console.ResetColor();
        }
    }
}
