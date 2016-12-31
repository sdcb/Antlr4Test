using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using static Antlr4Test.CalcCompiler.CalcCompilerParser;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;

namespace Antlr4Test.CalcCompiler
{
    class CalcCompilerProgram
    {
        public static void Run()
        {
            var input = File.ReadAllText("CalcCompiler/input.code");
            var inputStream = new AntlrInputStream(input);
            var lexer = new CalcCompilerLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CalcCompilerParser(tokenStream);

            var result = StatementVisitor.CreateMethod(parser.root());
            if (result.IsSuccess)
            {
                Console.WriteLine("编译成功.");
                result.Value();
            }
            else
            {
                Console.WriteLine(result.Error);
            }
        }

        public class StatementVisitor : CalcCompilerBaseVisitor<Result>
        {
            protected StatementVisitor(ILGenerator il, ISymbolDocumentWriter doc)
            {
                _il = il;
                _doc = doc;
            }

            private readonly ILGenerator _il;
            private Dictionary<string, LocalBuilder> _vars = new Dictionary<string, LocalBuilder>();
            private readonly ISymbolDocumentWriter _doc;

            public static Result<Action> CreateMethod(IParseTree tree)
            {
                var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndSave);

                Type daType = typeof(DebuggableAttribute);
                ConstructorInfo daCtor = daType.GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });
                CustomAttributeBuilder daBuilder = new CustomAttributeBuilder(daCtor, new object[] {
                    DebuggableAttribute.DebuggingModes.DisableOptimizations |
                    DebuggableAttribute.DebuggingModes.Default });
                assemblyBuilder.SetCustomAttribute(daBuilder);

                var moduleBuilder = assemblyBuilder.DefineDynamicModule("Program", "Program.exe", true);

                var doc = moduleBuilder.DefineDocument(@"CalcCompiler/input.code", Guid.Empty, Guid.Empty, Guid.Empty);

                var typeBuilder = moduleBuilder.DefineType("Foo", TypeAttributes.Public | TypeAttributes.Class);
                var method = typeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);

                var il = method.GetILGenerator();
                var visitor = new StatementVisitor(il, doc);
                var ok = visitor.Visit(tree);

                if (ok.IsSuccess)
                {
                    il.Emit(OpCodes.Ret);
                    var t = typeBuilder.CreateType();
                    assemblyBuilder.SetEntryPoint(method, PEFileKinds.ConsoleApplication);
                    assemblyBuilder.Save("Program.exe");
                    return Result.Ok((Action)t.GetMethod(method.Name).CreateDelegate(typeof(Action)));
                }
                else
                {
                    return Result.Fail<Action>(ok.Error);
                }
            }

            // Aggregate
            protected override Result AggregateResult(Result aggregate, Result nextResult)
            {
                if (aggregate != null && aggregate.IsFailure)
                {
                    return aggregate;
                }
                else if (nextResult == null)
                {
                    return Result.Ok();
                }
                else
                {
                    return nextResult;
                }
            }

            // statement
            public override Result VisitAssign([NotNull] AssignContext context)
            {
                var syntax = context.GetChild(0).GetText();
                var r = Visit(context.GetChild<ExpressionContext>(0));

                if (r.IsSuccess)
                {
                    _il.MarkSequencePoint(_doc, context.Start.Line, context.Start.Column, context.Stop.Line, context.Stop.Column);

                    LocalBuilder local;
                    if (_vars.ContainsKey(syntax))
                    {
                        local = _vars[syntax];
                    }
                    else
                    {
                        local = _il.DeclareLocal(typeof(double));
                        local.SetLocalSymInfo(syntax);
                        _vars[syntax] = local;
                    }

                    _il.Emit(OpCodes.Stloc, local);
                    return Result.Ok();
                }
                else
                {
                    return r;
                }
            }

            public override Result VisitCall([NotNull] CallContext context)
            {
                var syntax = context.GetChild(0).GetText();
                var v = Visit(context.GetChild<ExpressionContext>(0));

                if (v.IsSuccess)
                {
                    _il.MarkSequencePoint(_doc, context.Start.Line, context.Start.Column, context.Stop.Line, context.Stop.Column);

                    switch (syntax)
                    {
                        case "write":
                            _il.Emit(OpCodes.Call, typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(double) }));
                            break;
                        default:
                            return Result.Fail($"未知的一元函数：{syntax}.");
                    }
                    return Result.Ok();
                }
                else
                {
                    return v;
                }
            }

            // expression
            public override Result VisitNumber([NotNull] NumberContext context)
            {
                var v = double.Parse(context.GetText());
                _il.Emit(OpCodes.Ldc_R8, v);
                return Result.Ok();
            }

            public override Result VisitVariable([NotNull] CalcCompilerParser.VariableContext context)
            {
                var syntax = context.GetText();
                if (_vars.ContainsKey(syntax))
                {
                    var local = _vars[syntax];
                    _il.Emit(OpCodes.Ldloc, local);
                    return Result.Ok();
                }
                else
                {
                    return Result.Fail($"未定义的变量：{syntax}.");
                }
            }

            public override Result VisitBinary([NotNull] CalcCompilerParser.BinaryContext context)
            {
                var op = context.GetChild(1).GetText();
                var l = Visit(context.GetChild(0));
                var r = Visit(context.GetChild(2));

                if (l.IsSuccess && r.IsSuccess)
                {
                    switch (op)
                    {
                        case "+":
                            _il.Emit(OpCodes.Add);
                            break;
                        case "-":
                            _il.Emit(OpCodes.Sub);
                            break;
                        case "*":
                            _il.Emit(OpCodes.Mul);
                            break;
                        case "/":
                            _il.Emit(OpCodes.Div);
                            break;
                        case "%":
                            _il.Emit(OpCodes.Rem);
                            break;
                        case "^":
                            _il.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Pow),
                                new[] { typeof(double), typeof(double) }));
                            break;
                        default:
                            return Result.Fail($"未知的标点符号：{op}.");
                    }

                    return Result.Ok();
                }
                else
                {
                    return Result.Fail(l.Error + r.Error);
                }
            }

            public override Result VisitBinaryFunction([NotNull] BinaryFunctionContext context)
            {
                var func = context.GetChild(0).GetText();
                var l = Visit(context.GetChild<ExpressionContext>(0));
                var r = Visit(context.GetChild<ExpressionContext>(1));

                if (l.IsSuccess && r.IsSuccess)
                {
                    switch (func)
                    {
                        case "pow":
                            _il.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Pow), new[] { typeof(double), typeof(double) }));
                            break;
                        default:
                            return Result.Fail($"未知的二元函数：{func}.");
                    }

                    return Result.Ok();
                }
                else
                {
                    return Result.Fail(l.Error + r.Error);
                }
            }

            public override Result VisitFunction([NotNull] FunctionContext context)
            {
                var func = context.GetChild(0).GetText();
                var r = Visit(context.GetChild<ExpressionContext>(0));

                if (r.IsSuccess)
                {
                    switch (func)
                    {
                        case "abs":
                            _il.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(double) }));
                            break;
                        case "sin":
                            _il.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Sin), new[] { typeof(double) }));
                            break;
                        case "cos":
                            _il.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Cos), new[] { typeof(double) }));
                            break;
                        case "sqrt":
                            _il.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Sqrt), new[] { typeof(double) }));
                            break;
                        default:
                            return Result.Fail($"未知的一元函数：${func}.");
                    }

                    return Result.Ok();
                }
                else
                {
                    return r;
                }
            }

            public override Result VisitVoidFunction([NotNull] VoidFunctionContext context)
            {
                var func = context.GetChild(0).GetText();

                switch (func)
                {
                    case "read":
                        _il.Emit(OpCodes.Call, typeof(Console).GetMethod(nameof(Console.ReadLine)));
                        _il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToDouble), new[] { typeof(string) }));
                        break;
                    default:
                        return Result.Fail($"未知的一元函数：${func}.");
                }

                return Result.Ok();
            }

            public override Result VisitParenthesis([NotNull] ParenthesisContext context)
            {
                var r = Visit(context.GetChild<ExpressionContext>(0));
                return r;
            }
        }
    }
}
