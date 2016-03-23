using System.Collections.Generic;
using System.Linq;

namespace TrivialAst.Model
{
    public class AstFunction : AstNode
    {
        public string Function { get; private set; }
        public IReadOnlyList<AstNode> Arguments { get; private set; }

        public AstFunction(string function, IEnumerable<AstNode> arguments)
        {
            Function = function;
            Arguments = arguments.ToList().AsReadOnly();
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}