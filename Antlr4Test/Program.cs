using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using static Antlr4Test.CalcParser;

namespace Antlr4Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var input = "abs(-3) + 0.5 * pow(2, 4 / 2) + 3 / 4";
            var inputStream = new AntlrInputStream(input);
            var lexer = new CalcLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CalcParser(tokenStream);

            var visit = new Visitor();
            Console.WriteLine(visit.Visit(parser.root()));
        }

        class Visitor : CalcBaseVisitor<double>
        {
            public override double VisitNumber([NotNull] CalcParser.NumberContext context)
            {
                return double.Parse(context.GetText());
            }

            public override double VisitFunction([NotNull] FunctionContext context)
            {
                var func = context.GetChild(0).GetText();
                var v = Visit(context.GetChild<ExpressionContext>(0));

                switch (func)
                {
                    case "abs":
                        return Math.Abs(v);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(func));
                }
            }

            public override double VisitBinaryFunction([NotNull] BinaryFunctionContext context)
            {
                var func = context.GetChild(0).GetText();
                var v1 = Visit(context.GetChild<ExpressionContext>(0));
                var v2 = Visit(context.GetChild<ExpressionContext>(1));

                switch (func)
                {
                    case "pow":
                        return Math.Pow(v1, v2);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(func));
                }
            }

            public override double VisitBinary([NotNull] BinaryContext context)
            {
                var op = context.GetChild(1).GetText();
                var l = Visit(context.GetChild(0));
                var r = Visit(context.GetChild(2));

                switch (op)
                {
                    case "+":
                        return l + r;
                    case "-":
                        return l - r;
                    case "*":
                        return l * r;
                    case "/":
                        return l / r;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(op));
                }
            }
        }
    }
}
