using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProsthesisOS
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(ProsthesisCore.ProsthesisConstants.OSVersion);
            System.Threading.Thread.Sleep(1000);
        }
    }
}
