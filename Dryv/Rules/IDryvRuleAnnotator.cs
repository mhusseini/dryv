using System.Linq.Expressions;

namespace Dryv.Rules
{
    public interface IDryvRuleAnnotator
    {
        void Annotate(DryvRuleDefinition rule, LambdaExpression expression);
    }
}