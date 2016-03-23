namespace TrivialAst.Model
{
    public abstract class AstNode
    {
        public abstract T Accept<T>(IAstVisitor<T> visitor);
    }

    public class AstUnary : AstNode
    {
        public string Name { get; private set; }

        public AstNode Operand { get; private set; }

        public AstUnary(string name, AstNode operand)
        {
            Name = name;
            Operand = operand;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}