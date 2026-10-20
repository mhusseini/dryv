using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dryv.AspNetCore.Internal
{
    internal static class DryvExpressionHelper
    {
        public static List<ParameterExpression> FindParameters(Expression expression)
        {
            return new ParameterFinder().Find(expression);
        }

        internal class MemberFinder : ExpressionVisitor
        {
            private readonly Type type;

            public MemberFinder(Type type)
            {
                this.type = type;
            }

            public List<MemberExpression> FoundMemberExpressions { get; } = new List<MemberExpression>();

            protected override Expression VisitMember(MemberExpression node)
            {
                if (this.type.IsAssignableFrom(node.Member.DeclaringType))
                {
                    this.FoundMemberExpressions.Add(node);
                }

                return base.VisitMember(node);
            }
        }

        private class ParameterFinder : Dryv.Translation.Visitors.ExpressionNodeFinder<ParameterExpression>
        {
            private Expression entryNode;

            public List<ParameterExpression> Find(Expression node)
            {
                this.entryNode = node;
                base.Visit(node);

                return this.FoundChildren;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                if (this.entryNode != node)
                {
                    this.BlackList.AddRange(node.Parameters);
                }

                return base.VisitLambda<T>(node);
            }
        }
    }
}
