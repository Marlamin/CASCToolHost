using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace CASCToolHost
{
    public static class Logger
    {
        public static void WriteLine(string line)
        {
            ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out int availableAsyncIOThreads);

            Console.WriteLine("[" + DateTime.Now.ToString() + "] [AW=" + availableWorkerThreads + ", AIO=" + availableAsyncIOThreads + "] " + line);
        }

        public static void WriteLine(string line, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            WriteLine(line);
            Console.ResetColor();
        }
    }
}
