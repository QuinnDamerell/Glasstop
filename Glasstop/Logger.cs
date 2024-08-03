using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glasstop
{
    internal static class Logger
    {

        public static void Info(string msg, Exception e = null)
        {
            Console.WriteLine(msg);
        }

        public static void Error(string msg, Exception e = null)
        {
            Console.WriteLine(msg);
        }




    }
}
