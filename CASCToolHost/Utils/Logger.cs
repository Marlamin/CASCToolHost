using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace CASCToolHost
{
    public static class Logger
    {
        public static void WriteLine(string line)
        {
            ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out _);
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out _);

            Console.WriteLine("[" + DateTime.Now.ToString() + "] [AW=" + availableWorkerThreads + "/" + maxWorkerThreads + " (" + (maxWorkerThreads - availableWorkerThreads) + " active)] " + line);
        }

        public static void WriteLine(string line, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            WriteLine(line);
            Console.ResetColor();
        }
    }
}
