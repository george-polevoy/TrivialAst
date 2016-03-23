namespace TrivialAst.Model
{
    public class AstConditional : AstNode
    {
        public AstNode Condition { get; private set; }
        public AstNode IfTrue { get; private set; }
        public AstNode IfFalse { get; private set; }

        public AstConditional(AstNode condition, AstNode ifTrue, AstNode ifFalse)
        {
            Condition = condition;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}