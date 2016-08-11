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

namespace Antlr4Test.CalcCompiler
{
    class CalcCompilerProgram
    {
        public static void Run()
        {
            var input = File.ReadAllText("Calc/input.txt");
            var inputStream = new AntlrInputStream(input);
            var lexer = new CalcCompilerLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new CalcCompilerParser(tokenStream);

            StatementVisitor.CreateMethod(parser.root());
        }

        public class StatementVisitor : CalcCompilerBaseVisitor<Result>
        {
            protected StatementVisitor(ILGenerator il)
            {
                _il = il;
            }

            private ILGenerator _il;
            private Dictionary<string, LocalBuilder> _vars = new Dictionary<string, LocalBuilder>();

            public static Result<Action> CreateMethod(IParseTree tree)
            {
                var method = new DynamicMethod("Program", typeof(void), Type.EmptyTypes);
                var il = method.GetILGenerator();
                var visitor = new StatementVisitor(il);
                var ok = visitor.Visit(tree);

                if (ok.IsSuccess)
                {
                    return Result.Ok((Action)method.CreateDelegate(typeof(Action)));
                }
                else
                {
                    return Result.Fail<Action>(ok.Error);
                }
            }

            // statement


            // expression
            public override Result VisitNumber([NotNull] CalcCompilerParser.NumberContext context)
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
        }
    }
}
