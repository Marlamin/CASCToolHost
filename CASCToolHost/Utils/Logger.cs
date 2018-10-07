using System;

namespace CASCToolHost
{
    public static class Logger
    {
        public static void WriteLine(string line)
        {
            Console.WriteLine("[" + DateTime.Now.ToString() + "] " + line);
        }
    }
}
