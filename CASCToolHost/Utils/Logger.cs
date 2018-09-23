using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
