namespace TrivialAst.Model
{
    public abstract class DefaultAstVisitor<T> : IAstVisitor<T>
    {
        protected virtual T Default(AstNode node)
        {
            return default(T);
        }

        public virtual T Visit(AstFunction functionCall)
        {
            return Default(functionCall);
        }

        public virtual T Visit(AstParameter parameter)
        {
            return Default(parameter);
        }

        public virtual T Visit(AstBinary binary)
        {
            return Default(binary);
        }

        public virtual T Visit(AstConstant constant)
        {
            return Default(constant);
        }

        public virtual T Visit(AstConditional conditional)
        {
            return Default(conditional);
        }

        public virtual T Visit(AstUnsupported unsupported)
        {
            return Default(unsupported);
        }

        public T Visit(AstUnary unary)
        {
            throw new System.NotImplementedException();
        }
    }
}