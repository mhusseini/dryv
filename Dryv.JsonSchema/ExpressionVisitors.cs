using System.Linq.Expressions;

namespace Dryv.JsonSchema;

/// <summary>
/// Expression tree visitor utilities used during JSON Schema translation.
/// </summary>
internal static class ExpressionVisitors
{
    internal sealed class PropertyNameFinder(ParameterExpression? parameter) : ExpressionVisitor
    {
        public string? PropertyName { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (PropertyName == null && node.Expression == parameter)
            {
                PropertyName = node.Member.Name;
            }
            
            return base.VisitMember(node);
        }
    }

    internal sealed class ParameterFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Found = true;
            return base.VisitParameter(node);
        }
    }
}
