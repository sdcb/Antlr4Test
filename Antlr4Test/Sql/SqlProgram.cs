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
                new Person { Name = "Bush", Age = 12, BirthDay = DateTime.Parse("2015/1/1") },
                new Person { Name = "Shit", Age = 23, BirthDay = DateTime.Parse("2015/1/2") },
                new Person { Name = "Fuck", Age = 5, BirthDay = DateTime.Parse("2015/1/3") },
                new Person { Name = "Test", Age = 99, BirthDay = DateTime.Parse("2015/1/4") },
            };

            var visitor = new PredicateVisitor<Person>();
            var dataOut = visitor.DoQuery(persons.AsQueryable(), parser.run());

            foreach (var data in dataOut)
            {
                Console.WriteLine($"Name = {data.Name}, Age = {data.Age}, Date = {data.BirthDay}.");
            }
        }

        public class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }

            public DateTime BirthDay { get; set; }
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

            public override Expression VisitContains([NotNull] ContainsContext context)
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
                return Visit(context.GetChild<PredicateContext>(0));
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
                var v = (double)Visit(context.GetChild<ExpressionContext>(0));

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
                var v1 = (double)Visit(context.GetChild<ExpressionContext>(0));
                var v2 = (double)Visit(context.GetChild<ExpressionContext>(1));

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

            public override SqlValue VisitDate([NotNull] DateContext context)
            {
                return SqlValue.ParseDate(context.GetText());
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
                        return (double)l * (double)r;
                    case "/":
                        return (double)l / (double)r;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(op));
                }
            }
        }

        public struct SqlValue
        {
            private double? Number;

            private string String;

            private DateTime? Date;

            public SqlValues ValueType;

            public object Value()
            {
                if (ValueType == SqlValues.Number)
                {
                    return Number.Value;
                }
                else if (ValueType == SqlValues.String)
                {
                    return String;
                }
                else if (ValueType == SqlValues.Date)
                {
                    return Date;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(ValueType));
                }
            }

            public override string ToString()
            {
                if (ValueType == SqlValues.Number)
                {
                    return Number.Value.ToString();
                }
                else if (ValueType == SqlValues.String)
                {
                    return '"' + String + '"';
                }
                else if (ValueType == SqlValues.Date)
                {
                    return Date.Value.ToString();
                }
                throw new ArgumentOutOfRangeException(nameof(ValueType));
            }

            public static SqlValue ParseNumber(string text)
            {
                return new SqlValue
                {
                    ValueType = SqlValues.Number,
                    Number = double.Parse(text)
                };
            }

            public static SqlValue ParseNumber(double v)
            {
                return new SqlValue
                {
                    ValueType = SqlValues.Number,
                    Number = v
                };
            }

            public static SqlValue ParseDate(string text)
            {
                return new SqlValue
                {
                    ValueType = SqlValues.Date, 
                    Date = DateTime.Parse(text)
                };
            }

            public static explicit operator double(SqlValue v)
            {
                return v.Number.Value;
            }
            
            public static explicit operator string(SqlValue v)
            {
                if (v.String == null)
                    throw new NullReferenceException();
                return v.String;
            }

            public static explicit operator DateTime(SqlValue v)
            {
                return v.Date.Value;
            }

            public static implicit operator SqlValue(double v)
            {
                return ParseNumber(v);
            }

            public static implicit operator SqlValue(string v)
            {
                if (v == null)
                    throw new NullReferenceException();

                return new SqlValue
                {
                    ValueType = SqlValues.String,
                    String = v
                };
            }

            public static implicit operator SqlValue(DateTime v)
            {
                return new SqlValue
                {
                    ValueType = SqlValues.Date,
                    Date = v
                };
            }

            public static SqlValue operator +(SqlValue v1, SqlValue v2)
            {
                if (v1.ValueType == v2.ValueType)
                {
                    if (v1.ValueType == SqlValues.Number)
                    {
                        return (double)v1 + (double)v2;
                    }
                    else if (v1.ValueType == SqlValues.String)
                    {
                        return (string)v1 + (string)v2;
                    }
                }
                throw new NotSupportedException();
            }

            public static SqlValue operator -(SqlValue v1, SqlValue v2)
            {
                if (v1.ValueType == v2.ValueType)
                {
                    if (v1.ValueType == SqlValues.Number)
                    {
                        return (double)v1 - (double)v2;
                    }
                }
                throw new NotSupportedException();
            }
        }

        public enum SqlValues
        {
            Number,
            String, 
            Date
        }
    }
}
