namespace TrivialAst.Model
{
    public class AstUnsupported : AstNode
    {
        public string Explanation { get; set; }

        public AstUnsupported(string explanation)
        {
            Explanation = explanation;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
