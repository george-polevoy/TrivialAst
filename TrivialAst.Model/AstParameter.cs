namespace TrivialAst.Model
{
    public class AstParameter : AstNode
    {
        public string Name { get; private set; }

        public AstParameter(string name)
        {
            Name = name;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}