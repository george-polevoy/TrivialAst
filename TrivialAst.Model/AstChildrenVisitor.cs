using System.Collections.Generic;

namespace TrivialAst.Model
{
    public class AstChildrenVisitor : IAstVisitor<IEnumerable<AstNode>>
    {
        public IEnumerable<AstNode> Visit(AstFunction functionCall)
        {
            return functionCall.Arguments;
        }

        public IEnumerable<AstNode> Visit(AstParameter parameter)
        {
            yield break;
        }

        public IEnumerable<AstNode> Visit(AstBinary binary)
        {
            yield return binary.Left;
            yield return binary.Right;
        }

        public IEnumerable<AstNode> Visit(AstConstant constant)
        {
            yield break;
        }

        public IEnumerable<AstNode> Visit(AstConditional conditional)
        {
            yield return conditional.Condition;
            yield return conditional.IfTrue;
            yield return conditional.IfFalse;
        }

        public IEnumerable<AstNode> Visit(AstUnsupported unsupported)
        {
            yield break;
        }

        public IEnumerable<AstNode> Visit(AstUnary unary)
        {
            throw new System.NotImplementedException();
        }
    }
}