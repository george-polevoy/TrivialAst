namespace TrivialAst.Model
{
    public class AstBinary : AstNode
    {
        public string Name { get; private set; }
        public AstNode Left { get; private set; }
        public AstNode Right { get; private set; }

        public AstBinary(string name, AstNode left, AstNode right)
        {
            Name = name;
            Left = left;
            Right = right;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}