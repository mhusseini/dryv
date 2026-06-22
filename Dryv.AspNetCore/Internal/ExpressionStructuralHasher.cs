using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Dryv.AspNetCore.Internal
{
    /// <summary>
    /// Produces a deterministic canonical string from an expression tree's structure.
    /// Unlike <see cref="Expression.ToString()"/>, this output is independent of
    /// the C# compiler version and runtime platform, as it relies only on semantic
    /// node types, method signatures, member names, and constant values.
    /// </summary>
    internal sealed class ExpressionStructuralHasher : ExpressionVisitor
    {
        private readonly StringBuilder sb = new StringBuilder();

        public static string GetStructuralHash(Expression expression)
        {
            var hasher = new ExpressionStructuralHasher();
            hasher.Visit(expression);
            return hasher.sb.ToString();
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            sb.Append('(');
            Visit(node.Left);
            sb.Append(node.NodeType.ToString());
            Visit(node.Right);
            sb.Append(')');
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            sb.Append(node.NodeType.ToString());
            sb.Append('(');
            Visit(node.Operand);
            sb.Append(')');
            return node;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            sb.Append("IIF(");
            Visit(node.Test);
            sb.Append(',');
            Visit(node.IfTrue);
            sb.Append(',');
            Visit(node.IfFalse);
            sb.Append(')');
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value == null)
            {
                sb.Append("null");
            }
            else
            {
                var type = node.Value.GetType();
                if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                {
                    sb.Append(node.Value);
                }
                else if (IsCompilerGenerated(type))
                {
                    // Compiler-generated closure classes have unstable names
                    // (e.g. <>c__DisplayClass0_0). Use a stable marker instead.
                    // The MemberExpression visiting this constant will append
                    // the stable field name.
                    sb.Append("closure");
                }
                else
                {
                    // For other non-primitive constants, use the full type name
                    sb.Append("const<");
                    sb.Append(GetStableTypeName(type));
                    sb.Append('>');
                }
            }
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Use type + name for stability (parameter names come from source code)
            sb.Append("param<");
            sb.Append(GetStableTypeName(node.Type));
            sb.Append(">:");
            sb.Append(node.Name);
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression != null)
            {
                Visit(node.Expression);
            }
            else
            {
                // Static member access
                sb.Append(GetStableTypeName(node.Member.DeclaringType));
            }
            sb.Append('.');
            sb.Append(node.Member.Name);
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Object != null)
            {
                Visit(node.Object);
                sb.Append('.');
            }
            else
            {
                // Static method
                sb.Append(GetStableTypeName(node.Method.DeclaringType));
                sb.Append('.');
            }

            sb.Append(node.Method.Name);
            sb.Append('(');

            for (var i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0) sb.Append(',');
                Visit(node.Arguments[i]);
            }

            sb.Append(')');
            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            sb.Append("lambda(");
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                if (i > 0) sb.Append(',');
                VisitParameter(node.Parameters[i]);
            }
            sb.Append(")=>");
            Visit(node.Body);
            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            sb.Append("new ");
            sb.Append(GetStableTypeName(node.Type));
            sb.Append('(');
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0) sb.Append(',');
                Visit(node.Arguments[i]);
            }
            sb.Append(')');
            return node;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            VisitNew(node.NewExpression);
            sb.Append('{');
            for (var i = 0; i < node.Bindings.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var binding = node.Bindings[i];
                sb.Append(binding.Member.Name);
                if (binding is MemberAssignment assignment)
                {
                    sb.Append('=');
                    Visit(assignment.Expression);
                }
            }
            sb.Append('}');
            return node;
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            Visit(node.Object);
            sb.Append('[');
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0) sb.Append(',');
                Visit(node.Arguments[i]);
            }
            sb.Append(']');
            return node;
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            Visit(node.Expression);
            sb.Append(" is ");
            sb.Append(GetStableTypeName(node.TypeOperand));
            return node;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            sb.Append("invoke(");
            Visit(node.Expression);
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                sb.Append(',');
                Visit(node.Arguments[i]);
            }
            sb.Append(')');
            return node;
        }

        protected override Expression VisitDefault(DefaultExpression node)
        {
            sb.Append("default(");
            sb.Append(GetStableTypeName(node.Type));
            sb.Append(')');
            return node;
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            sb.Append("new ");
            sb.Append(GetStableTypeName(node.Type.GetElementType()));
            sb.Append("[]{");
            for (var i = 0; i < node.Expressions.Count; i++)
            {
                if (i > 0) sb.Append(',');
                Visit(node.Expressions[i]);
            }
            sb.Append('}');
            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            sb.Append("block{");
            foreach (var expr in node.Expressions)
            {
                Visit(expr);
                sb.Append(';');
            }
            sb.Append('}');
            return node;
        }

        private static bool IsCompilerGenerated(Type type)
        {
            return type.Name.Contains("<>") || type.Name.Contains("__") ||
                   Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute));
        }

        private static string GetStableTypeName(Type type)
        {
            if (type == null) return "?";
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(double)) return "double";
            if (type == typeof(float)) return "float";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(object)) return "object";
            if (type == typeof(void)) return "void";

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(Nullable<>))
                {
                    return GetStableTypeName(type.GetGenericArguments()[0]) + "?";
                }

                var baseName = type.FullName?.Split('`')[0] ?? type.Name.Split('`')[0];
                var args = type.GetGenericArguments();
                return baseName + "<" + string.Join(",", Array.ConvertAll(args, GetStableTypeName)) + ">";
            }

            // Use FullName for stability; fall back to Name for compiler-generated types
            return type.FullName ?? type.Name;
        }
    }
}
