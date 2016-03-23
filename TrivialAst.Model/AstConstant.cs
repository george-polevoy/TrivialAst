namespace TrivialAst.Model
{
    public class AstConstant : AstNode
    {
        public object Value { get; private set; }

        public AstConstant(object value)
        {
            Value = value;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}