using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

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
        }
    }
}
