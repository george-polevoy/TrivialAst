namespace TrivialAst.Model
{
    public interface IAstVisitor<out T>
    {
        T Visit(AstFunction functionCall);
        T Visit(AstParameter parameter);
        T Visit(AstBinary binary);
        T Visit(AstConstant constant);
        T Visit(AstConditional conditional);
        T Visit(AstUnsupported unsupported);
        T Visit(AstUnary unary);
    }
}