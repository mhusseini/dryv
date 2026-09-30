using System;
using System.Linq.Expressions;

namespace Dryv.Translation.Visitors
{
    internal class NullableMemberModifier : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.Name != nameof(Nullable<int>.HasValue) || !IsNullable(node.Expression))
            {
                return base.VisitMember(node);
            }

            var nullable = this.Visit(node.Expression);

            return Expression.NotEqual(nullable, Expression.Constant(null, nullable.Type));
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name != nameof(Nullable<int>.GetValueOrDefault) || !IsNullable(node.Object))
            {
                return base.VisitMethodCall(node);
            }

            var nullable = this.Visit(node.Object);
            var defaultValue = node.Arguments.Count == 0
                ? Expression.Default(node.Type)
                : this.Visit(node.Arguments[0]);

            return Expression.Coalesce(nullable, defaultValue);
        }

        private static bool IsNullable(Expression expression)
        {
            return expression != null && Nullable.GetUnderlyingType(expression.Type) != null;
        }
    }
}
