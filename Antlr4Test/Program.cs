using Antlr4Test.Calc;
using Antlr4Test.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: CLSCompliant(false)]

namespace Antlr4Test
{
    class Program
    {
        public static void Main()
        {
            Console.WriteLine("Calc: ");
            CalcProgram.Run();

            //Console.WriteLine("\r\nSQL: ");
            //SqlProgram.Run();
        }
    }
}
