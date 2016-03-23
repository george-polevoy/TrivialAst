using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TrivialAst.Model;

namespace TrivialAst.Parser.CSharp
{
    /// <summary>
    /// Expression parsing.
    /// https://github.com/dotnet/roslyn/wiki/Getting-Started-C%23-Syntax-Analysis
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        Parser()
        {
        }

        public static AstNode Parse(string expression)
        {
            return new Parser().Analyse(expression);
        }

        public AstNode Analyse(string expression)
        {
            var tree = CSharpSyntaxTree.ParseText(expression, new CSharpParseOptions(LanguageVersion.CSharp6, DocumentationMode.Parse, SourceCodeKind.Script));

            var root = (CompilationUnitSyntax)tree.GetRoot();

            VisitExpression(root, 0);

            var walker = new Walker();

            return walker.Walk(root);
        }

        void Report(string what, int level)
        {
            Console.WriteLine(string.Join("", Enumerable.Repeat("\t", level)) + what);
        }

        class Walker : CSharpSyntaxWalker
        {
            Queue<Func<AstNode>> q = new Queue<Func<AstNode>>();

            public AstNode Walk(SyntaxNode node)
            {
                Visit(node);

                if (q.Count != 0)
                    return q.Dequeue()();
                return null;
            }

            IEnumerable<AstNode> Dequeue(int count)
            {
                for (var i = 0; i < count; i++)
                    yield return q.Dequeue()();
            }

            AstNode Dequeue()
            {
                return q.Dequeue()();
            }

            void Enqueue(Func<AstNode> func)
            {
                q.Enqueue(func);
            }

            public override void VisitCastExpression(CastExpressionSyntax node)
            {
                Enqueue(() => new AstUnsupported("Type conversion is not supported"));
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                var memberAccess = (MemberAccessExpressionSyntax)node.Expression;
                Enqueue(() => new AstFunction(memberAccess.ToFullString().Trim(), Dequeue(node.ArgumentList.Arguments.Count)));
                foreach (var arg in node.ArgumentList.Arguments)
                    Visit(arg);
            }

            public override void VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                Enqueue(() => new AstConstant(node.Token.Value));
                base.VisitLiteralExpression(node);
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                Enqueue(() => new AstBinary(node.OperatorToken.ToFullString().Trim(), Dequeue(), Dequeue()));
                base.VisitBinaryExpression(node);
            }

            public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
            {
                Enqueue(() => new AstUnary(node.OperatorToken.ToFullString().Trim(), Dequeue()));
                base.VisitPrefixUnaryExpression(node);
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                Enqueue(() => new AstParameter(node.Identifier.ToFullString().Trim()));
                base.VisitIdentifierName(node);
            }

            public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
            {
                Enqueue(() => new AstConditional(Dequeue(), Dequeue(), Dequeue()));
                base.VisitConditionalExpression(node);
            }
        }

        void VisitExpression(SyntaxNode node, int level)
        {
            Report(node.Kind() + ": " + node.ToFullString(), level);
            foreach (var childNode in node.ChildNodes())
            {
                VisitExpression(childNode, level + 1);
            }
        }
    }
}
