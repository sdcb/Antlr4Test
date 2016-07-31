using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using static Antlr4Test.Sql.ExpressionParser;
using System.IO;
using Antlr4.Runtime;

namespace Antlr4Test.Sql
{
    class SqlProgram
    {
        public static void Run()
        {
            var input = File.ReadAllText("Sql/input.txt");
            var inputStream = new AntlrInputStream(input);
            var lexer = new ExpressionLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new ExpressionParser(tokenStream);
            var visitor = new ExpressionVisitor();
            Console.WriteLine(visitor.Visit(parser.expression()).ToString());
        }

        public class ExpressionVisitor : ExpressionBaseVisitor<Value>
        {
            public override Value VisitExpressionParenthesis([NotNull] ExpressionParenthesisContext context)
            {
                return Visit(context.GetChild<ExpressionContext>(0));
            }

            public override Value VisitNumber([NotNull] NumberContext context)
            {
                return Value.ParseNumber(context.GetText());
            }

            public override Value VisitFunction([NotNull] FunctionContext context)
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

            public override Value VisitBinaryFunction([NotNull] BinaryFunctionContext context)
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

            public override Value VisitString([NotNull] StringContext context)
            {
                var text = context.GetText();
                return text.Substring(1, text.Length-2);
            }

            public override Value VisitBinary([NotNull] BinaryContext context)
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

        public struct Value
        {
            public double? Number;

            public string String;

            public ValueTypes ValueTypes;

            public override string ToString()
            {
                if (ValueTypes == ValueTypes.Number)
                {
                    return Number.Value.ToString();
                }
                else if (ValueTypes == ValueTypes.String)
                {
                    return '"' + String + '"';
                }
                throw new ArgumentOutOfRangeException(nameof(ValueTypes));
            }

            public static Value ParseNumber(string text)
            {
                return new Value
                {
                    ValueTypes = ValueTypes.Number,
                    Number = double.Parse(text)
                };
            }

            public static Value ParseNumber(double v)
            {
                return new Value
                {
                    ValueTypes = ValueTypes.Number,
                    Number = v
                };
            }

            public static implicit operator double(Value v)
            {
                return v.Number.Value;
            }

            [return: NotNull]
            public static implicit operator string(Value v)
            {
                if (v.String == null)
                    throw new NullReferenceException();
                return v.String;
            }

            public static implicit operator Value(double v)
            {
                return ParseNumber(v);
            }

            [return: NotNull]
            public static implicit operator Value([NotNull]string v)
            {
                if (v == null)
                    throw new NullReferenceException();

                return new Value
                {
                    ValueTypes = ValueTypes.String,
                    String = v
                };
            }

            public static Value operator +(Value v1, Value v2)
            {
                if (v1.ValueTypes == v2.ValueTypes)
                {
                    if (v1.ValueTypes == ValueTypes.Number)
                    {
                        return (double)v1 + (double)v2;
                    }
                    else if (v1.ValueTypes == ValueTypes.String)
                    {
                        return (string)v1 + (string)v2;
                    }
                }
                throw new ArgumentOutOfRangeException();
            }
        }

        public enum ValueTypes
        {
            Number,
            String
        }
    }
}
