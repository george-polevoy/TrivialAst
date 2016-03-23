using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using TrivialAst.Model;

namespace TrivialAst.Parser.CSharp
{
    public class ParserTests
    {
        class DebugVisitor : IAstVisitor<string>
        {
            public string Visit(AstFunction functionCall)
            {
                return string.Format("{0}({1})", functionCall.Function, string.Join(", ", functionCall.Arguments.Select(i => i.Accept(this))));
            }

            public string Visit(AstParameter parameter)
            {
                return parameter.Name;
            }

            public string Visit(AstBinary binary)
            {
                return string.Format("({0} {1} {2})", binary.Left.Accept(this), binary.Name, binary.Right.Accept(this));
            }

            public string Visit(AstConstant constant)
            {
                return RenderLiteral(constant.Value);
            }

            public string Visit(AstConditional conditional)
            {
                return string.Format("({0}) ? ({1}) : ({2})",
                    conditional.Condition.Accept(this),
                    conditional.IfTrue.Accept(this),
                    conditional.IfFalse.Accept(this));
            }

            public string Visit(AstUnsupported unsupported)
            {
                return "<error>";
            }

            public string Visit(AstUnary unary)
            {
                return string.Format("{0} {1}", unary.Name, unary.Operand.Accept(this));
            }

            private string RenderLiteral(object o)
            {
                switch (o.GetType().ToString())
                {
                    case "System.String":
                        return string.Format(@"""{0}""", o);
                    case "System.Decimal":
                    case "System.Int32":
                    case "System.Int64":
                    case "System.Float":
                    case "System.Double":
                        return Convert.ToDecimal(o).ToString(CultureInfo.InvariantCulture);
                    case "System.Boolean":
                        return (bool) o ? "true" : "false";
                    default:
                        throw new NotSupportedException(string.Format("Literal value {0} has unsupported type {1}", o, o.GetType()));
                }
            }
        }

        static class AstDocumentOrderTraverser
        {
            public static IEnumerable<AstNode> Traverse(AstNode node)
            {
                yield return node;
                foreach (var child in node.Accept(new AstChildrenVisitor()))
                    foreach (var childTraverse in Traverse(child)) yield return childTraverse;
            }
        }

        class AstIsErrorVisitor : DefaultAstVisitor<bool>
        {
            protected override bool Default(AstNode node)
            {
                return false;
            }

            public override bool Visit(AstUnsupported unsupported)
            {
                return true;
            }
        }

        class Functions
        {
            public static object Sin(object o)
            {
                return Math.Sin(Convert.ToDouble(o));
            }

            public static object Negate(object o)
            {
                if (o is bool)
                {
                    return !(bool) o;
                }

                return -Convert.ToDecimal(o);
            }
        }

        class ExpressionGenerator : IAstVisitor<Expression>
        {
            private List<ParameterExpression> parameters = new List<ParameterExpression>();

            public IReadOnlyList<ParameterExpression> GetParameters()
            {
                return parameters.AsReadOnly();
            }

            protected MethodInfo ResolveFunction(string function)
            {
                switch (function)
                {
                    case "Math.Sin":
                        return typeof(Functions).GetMethod("Sin", BindingFlags.Static | BindingFlags.Public);
                    default:
                        throw new NotSupportedException(string.Format("Function {0} is not supported", function));
                }
            }

            private ExpressionType ResolveBinaryType(string op)
            {
                switch (op)
                {
                    case "+":
                        return ExpressionType.Add;
                    case "-":
                        return ExpressionType.Subtract;
                    case "*":
                        return ExpressionType.Multiply;
                    case "/":
                        return ExpressionType.Divide;

                    default:
                        throw new NotSupportedException(string.Format("Operator {0} is not supported", op));
                }
            }

            public Expression Visit(AstFunction functionCall)
            {
                var resolveFunction = ResolveFunction(functionCall.Function);
                return Expression.Call(resolveFunction,
                    functionCall.Arguments.Select(a => a.Accept(this)));
            }

            public Expression Visit(AstParameter parameter)
            {
                var parameterExpression = Expression.Parameter(typeof (object), parameter.Name);
                parameters.Add(parameterExpression);
                return parameterExpression;
            }

            public Expression Visit(AstBinary binary)
            {
                return Expression.MakeBinary(ResolveBinaryType(binary.Name), binary.Left.Accept(this),
                    binary.Right.Accept(this));
            }

            public Expression Visit(AstUnary unary)
            {
                switch (unary.Name)
                {
                    case "-":
                    case "!":
                        return Expression.Call(typeof(Functions).GetMethod("Negate", BindingFlags.Static | BindingFlags.Public), unary.Operand.Accept(this));
                    default:
                        throw new NotSupportedException(string.Format("Operator {0} is not supported", unary.Name));
                }
            }

            public Expression Visit(AstConstant constant)
            {
                return Expression.Constant(constant.Value, typeof(object));
            }

            public Expression Visit(AstConditional conditional)
            {
                return Expression.IfThenElse(conditional.Condition.Accept(this), conditional.IfTrue.Accept(this),
                    conditional.IfFalse.Accept(this));
            }

            public Expression Visit(AstUnsupported unsupported)
            {
                throw new NotSupportedException(unsupported.Explanation);
            }
        }

        public class ExpressionParameter
        {
            public string Name { get; set; }
            public object Value { get; set; }

            public ExpressionParameter(string name, object value)
            {
                Name = name;
                Value = value;
            }
        }

        public class ExpressionInterpreter : IAstVisitor<object>
        {
            private Func<string, IEnumerable<object>, object> EvaluateFunction { get; set; }
            private Dictionary<string, object> Parameters { get; set; }

            private ExpressionInterpreter(IEnumerable<ExpressionParameter> parameters, Func<string, IEnumerable<object>, object> evaluateFunction)
            {
                EvaluateFunction = evaluateFunction;
                Parameters = (parameters ?? Enumerable.Empty<ExpressionParameter>()).ToDictionary(i => i.Name, i => i.Value);
            }

            public static object Compute(AstNode function, IEnumerable<ExpressionParameter> parameters, Func<string, IEnumerable<object>, object> evaluateFunction)
            {
                return function.Accept(new ExpressionInterpreter(parameters, evaluateFunction));
            }

            public object Visit(AstFunction functionCall)
            {
                return EvaluateFunction(functionCall.Function, functionCall.Arguments.Select(p => p.Accept(this)));
            }

            public object Visit(AstParameter parameter)
            {
                return Parameters[parameter.Name];
            }

            public object Visit(AstBinary binary)
            {
                return GetDecimalOp(binary)(EvaluateDecimal(binary.Left), EvaluateDecimal(binary.Right));
            }

            private Func<decimal, decimal, decimal> GetDecimalOp(AstBinary binary)
            {
                switch (binary.Name)
                {
                    case "+":
                        return (a, b) => a + b;
                    case "-":
                        return (a, b) => a - b;
                    case "*":
                        return (a, b) => a * b;
                    case "/":
                        return (a, b) => a / b;
                    default:
                        throw new NotSupportedException(string.Format("Binary operator is not supported: {0}", binary.Name));
                }
            }

            decimal EvaluateDecimal(AstNode n)
            {
                return Convert.ToDecimal(n.Accept(this));
            }

            public object Visit(AstConstant constant)
            {
                return constant.Value;
            }

            public object Visit(AstConditional conditional)
            {
                return (bool) conditional.Condition.Accept(this)
                    ? conditional.IfTrue.Accept(this)
                    : conditional.IfFalse.Accept(this);
            }

            public object Visit(AstUnsupported unsupported)
            {
                throw new NotSupportedException(unsupported.Explanation);
            }

            public object Visit(AstUnary unary)
            {
                switch (unary.Name)
                {
                    case "-":
                        return -Convert.ToDecimal(unary.Operand.Accept(this));
                    case "!":
                        return !(bool) unary.Operand.Accept(this);

                    default:
                        throw new NotSupportedException(string.Format("Unary operator is not supported: {0}", unary.Name));
                }
            }
        }

        [Test]
        public void ExpressionInterpreterTest()
        {
            var actual = ExpressionInterpreter.Compute(Parser.Parse("- 2 * z / Math.Pow(2, x * PI)"),
                new[] {new ExpressionParameter("x", 1.0), new ExpressionParameter("z", 2.0), new ExpressionParameter("PI", Math.PI), }, (name, parameters) =>
                {
                    switch (name)
                    {
                        case "Math.Pow":
                        {
                            var args = parameters.Select(Convert.ToDouble).ToList();
                            if (args.Count != 2)
                                throw new InvalidOperationException("Wrong number of arguments passed to Math.Pow. Expected (double x, double y)");
                            return (decimal) Math.Pow(args[0], args[1]);
                        }
                        default:
                            throw new NotSupportedException(string.Format("Function is not supported: {0}", name));
                    }
                });

            Assert.AreEqual(-0.4532589291870443972585070297m, actual);
        }

        [Test]
        public void ExpressionCalculatorUnary()
        {
            var ast = new AstUnary("-", new AstConstant(1));
            var f = MakeFunc<Func<object>>(ast);
            Assert.AreEqual(-1, f());
        }

        [Test]
        public void ExpressionCalculator()
        {
            var ast = new AstFunction("Math.Sin", new []{new AstParameter("x") });
            var f = MakeFunc<Func<object, object>>(ast);
            Assert.AreEqual(1.0, f((Math.PI / 2).ToString()));
            Assert.AreEqual(1.0, f(Math.PI / 2));
        }

        private static T MakeFunc<T>(AstNode astFunction)
        {
            var generator = new ExpressionGenerator();
            var expressionBody = astFunction.Accept(generator);
            var lambda = Expression.Lambda<T>(expressionBody, generator.GetParameters());
            var f = lambda.Compile(DebugInfoGenerator.CreatePdbGenerator());
            return f;
        }

        [Test]
        public void ErrorCollection()
        {
            var node = new AstFunction("foo", new AstNode[] {new AstUnsupported("some error")});
            var errorNodes = AstDocumentOrderTraverser.Traverse(node).Where(i => i.Accept(new AstIsErrorVisitor())).ToList();
            Assert.AreEqual(1, errorNodes.Count);
        }

        [Test]
        public void TestConversion()
        {
            Assert.AreEqual("(1 + <error>)", Parser.Parse("1 + (int)2.0").Accept(new DebugVisitor()));
        }

        [Test]
        public void TestCalculator()
        {
            Assert.AreEqual("(1 + 2000)", Parser.Parse("1 + 2e3").Accept(new DebugVisitor()));
        }

        [Test]
        public void TestUnary()
        {
            Assert.AreEqual("- 1", Parser.Parse("-1").Accept(new DebugVisitor()));
        }

        [Test]
        public void TestBinary()
        {
            Assert.AreEqual("(1 + 2)", Parser.Parse("1+2").Accept(new DebugVisitor()));
        }

        [Test]
        [Explicit]
        public void TestParser()
        {
            var ast = Parser.Parse(@"Foo.Bar(-x * z, y + (Bar.Foo(x) > y + 1) || true ? 1m : 2m)");

            Console.WriteLine( ast.Accept(new DebugVisitor()));
        }
    }
}
