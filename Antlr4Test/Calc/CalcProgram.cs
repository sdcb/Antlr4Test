using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using static Antlr4Test.CalcParser;
using System.IO;

namespace Antlr4Test.Calc
{
    class CalcProgram
    {
        public static void Run()
        {
            var input = File.ReadAllText("Calc/input.txt");
            var inputStream = new AntlrInputStream(input);
            var lexer = new CalcLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CalcParser(tokenStream);

            var visit = new StatementVisitor();
            //Console.WriteLine(parser.root().GetText());
            visit.Visit(parser.root());
        }

        class StatementVisitor : CalcBaseVisitor<double>
        {
            Dictionary<string, double> vars = new Dictionary<string, double>();
            
            public override double VisitAssign([NotNull] AssignContext context)
            {
                var syntax = context.GetChild(0).GetText();
                var exp = context.GetChild<ExpressionContext>(0);
                SetVar(syntax, EvalStatement(exp));
                return 0;
            }
            
            public override double VisitCall([NotNull] CallContext context)
            {
                var syntax = context.GetChild(0).GetText();
                var exp = EvalStatement(context.GetChild<ExpressionContext>(0));

                switch (syntax)
                {
                    case "write":
                        Console.WriteLine(exp);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(syntax));
                }
                return 0;
            }

            private void SetVar([NotNull]string syntax, double value)
            {
                vars[syntax] = value;
            }

            private double GetVar([NotNull]string syntax)
            {
                return vars[syntax];
            }

            public double EvalStatement([NotNull]ExpressionContext expression)
            {
                return new ExpressionVisitor(GetVar).Visit(expression);
            }
        }

        class ExpressionVisitor : CalcBaseVisitor<double>
        {
            public Func<string, double> VarGetter { get; }

            public ExpressionVisitor([NotNull]Func<string, double> varGetter)
            {
                VarGetter = varGetter;
            }

            public override double VisitParenthesis([NotNull] ParenthesisContext context)
            {
                return Visit(context.GetChild<ExpressionContext>(0));
            }

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

            public override double VisitVariable([NotNull] VariableContext context)
            {
                var syntax = context.GetText();
                return VarGetter(syntax);
            }
        }
    }
}
