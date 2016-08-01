using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using System.IO;
using Antlr4.Runtime;
using static Antlr4Test.Sql.SqlParser;
using System.Linq.Expressions;
using Antlr4.Runtime.Tree;
using System.Reflection;

namespace Antlr4Test.Sql
{
    class SqlProgram
    {
        public static void Run()
        {
            var input = File.ReadAllText("Sql/input.txt");
            var inputStream = new AntlrInputStream(input);
            var lexer = new SqlLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new SqlParser(tokenStream);

            var persons = new List<Person>
            {
                new Person { Name = "Bush", Age = 12 },
                new Person { Name = "Shit", Age = 23 },
                new Person { Name = "Fuck", Age = 5 },
                new Person { Name = "Test", Age = 99 },
            };

            var visitor = new PredicateVisitor<Person>();
            var dataOut = visitor.DoQuery(persons.AsQueryable(), parser.run());

            foreach (var data in dataOut)
            {
                Console.WriteLine($"Name = {data.Name}, Age = {data.Age}.");
            }
        }

        public class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }

        public class PredicateVisitor<T> : SqlBaseVisitor<Expression>
        {
            ParameterExpression Pe = Expression.Parameter(typeof(T), "x");

            public override Expression VisitSingleOperator([NotNull] SingleOperatorContext context)
            {
                var syntax = context.GetChild(0).GetText();
                var opText = context.GetChild(1).GetText();
                var val = EvalExpression(context.GetChild(2));

                var left = Expression.PropertyOrField(Pe, syntax);
                var right = Expression.Constant(Convert.ChangeType(val.Value(), left.Type));

                Expression op;
                switch (opText)
                {
                    case "=":
                        op = Expression.Equal(left, right);
                        break;
                    case "!=":
                        op = Expression.NotEqual(left, right);
                        break;
                    case ">":
                        op = Expression.GreaterThan(left, right);
                        break;
                    case "<":
                        op = Expression.LessThan(left, right);
                        break;
                    case ">=":
                        op = Expression.GreaterThanOrEqual(left, right);
                        break;
                    case "<=":
                        op = Expression.LessThanOrEqual(left, right);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(opText));
                }
                return op;
            }

            public override Expression VisitAndOr([NotNull] AndOrContext context)
            {
                var left = Visit(context.GetChild<PredicateContext>(0));
                var right = Visit(context.GetChild<PredicateContext>(1));
                var opText = context.GetChild(1).GetText();

                switch (opText.ToUpperInvariant())
                {
                    case "AND":
                        return Expression.AndAlso(left, right);
                    case "OR":
                        return Expression.OrElse(left, right);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(opText));
                }
            }

            public override Expression VisitBetween([NotNull] BetweenContext context)
            {
                var syntax = context.GetChild(0).GetText();
                var exp1 = EvalExpression(context.GetChild<ExpressionContext>(0)).Value();
                var exp2 = EvalExpression(context.GetChild<ExpressionContext>(1)).Value();

                var prop = Expression.PropertyOrField(Pe, syntax);
                var left = Expression.Constant(Convert.ChangeType(exp1, prop.Type));
                var right = Expression.Constant(Convert.ChangeType(exp2, prop.Type));

                return Expression.AndAlso(
                    Expression.LessThanOrEqual(left, prop), 
                    Expression.LessThan(prop, right)
                    );
            }

            public override Expression VisitIn([NotNull] InContext context)
            {
                var syntax = context.GetChild(0).GetText();
                var values = context.children
                    .OfType<ExpressionContext>()
                    .Select(x => EvalExpression(x).Value())
                    .ToList();

                var left = Expression.Constant(values);
                var right = Expression.PropertyOrField(Pe, syntax);
                var op = Expression.Call(
                    left,
                    typeof(List<object>).GetMethod("Contains", new[] { typeof(object) }),
                    right);
                return op;
            }

            public override Expression VisitParenthesis([NotNull] ParenthesisContext context)
            {
                return Visit(context.GetChild<ExpressionContext>(0));
            }

            public IQueryable<T> DoQuery(IQueryable<T> dataIn, IParseTree tree)
            {
                var predicate = Visit(tree);
                var where = Expression.Call(
                    typeof(Queryable),
                    "Where",
                    new Type[] { dataIn.ElementType },
                    dataIn.Expression,
                    Expression.Lambda<Func<T, bool>>(predicate, new ParameterExpression[] { Pe }));
                return dataIn.Provider.CreateQuery<T>(where);
            }

            private SqlValue EvalExpression(IParseTree expression)
            {
                return new ExpressionVisitor().Visit(expression);
            }
        }

        public class ExpressionVisitor : SqlBaseVisitor<SqlValue>
        {
            public override SqlValue VisitExpressionParenthesis([NotNull] ExpressionParenthesisContext context)
            {
                return Visit(context.GetChild<ExpressionContext>(0));
            }

            public override SqlValue VisitNumber([NotNull] NumberContext context)
            {
                return SqlValue.ParseNumber(context.GetText());
            }

            public override SqlValue VisitFunction([NotNull] FunctionContext context)
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

            public override SqlValue VisitBinaryFunction([NotNull] BinaryFunctionContext context)
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

            public override SqlValue VisitString([NotNull] StringContext context)
            {
                var text = context.GetText();
                return text.Substring(1, text.Length - 2);
            }

            public override SqlValue VisitBinary([NotNull] BinaryContext context)
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

        public struct SqlValue
        {
            public double? Number;

            public string String;

            public ValueTypes ValueType;

            public object Value()
            {
                if (ValueType == ValueTypes.Number)
                {
                    return Number.Value;
                }
                else if (ValueType == ValueTypes.String)
                {
                    return String;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(ValueType));
                }
            }

            public override string ToString()
            {
                if (ValueType == ValueTypes.Number)
                {
                    return Number.Value.ToString();
                }
                else if (ValueType == ValueTypes.String)
                {
                    return '"' + String + '"';
                }
                throw new ArgumentOutOfRangeException(nameof(ValueType));
            }

            public static SqlValue ParseNumber(string text)
            {
                return new SqlValue
                {
                    ValueType = ValueTypes.Number,
                    Number = double.Parse(text)
                };
            }

            public static SqlValue ParseNumber(double v)
            {
                return new SqlValue
                {
                    ValueType = ValueTypes.Number,
                    Number = v
                };
            }

            public static implicit operator double(SqlValue v)
            {
                return v.Number.Value;
            }

            [return: NotNull]
            public static implicit operator string(SqlValue v)
            {
                if (v.String == null)
                    throw new NullReferenceException();
                return v.String;
            }

            public static implicit operator SqlValue(double v)
            {
                return ParseNumber(v);
            }

            [return: NotNull]
            public static implicit operator SqlValue([NotNull]string v)
            {
                if (v == null)
                    throw new NullReferenceException();

                return new SqlValue
                {
                    ValueType = ValueTypes.String,
                    String = v
                };
            }

            public static SqlValue operator +(SqlValue v1, SqlValue v2)
            {
                if (v1.ValueType == v2.ValueType)
                {
                    if (v1.ValueType == ValueTypes.Number)
                    {
                        return (double)v1 + (double)v2;
                    }
                    else if (v1.ValueType == ValueTypes.String)
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
