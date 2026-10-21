using System.Linq.Expressions;

namespace Dryv.Rules
{
    /// <summary>
    /// Allows external code to attach custom annotations to a <see cref="DryvRuleDefinition"/>
    /// during rule discovery, before the rule is compiled or translated.
    /// </summary>
    public interface IDryvRuleAnnotator
    {
        /// <summary>
        /// Inspects the given <paramref name="expression"/> and adds entries to
        /// <paramref name="rule"/>.<see cref="DryvRuleDefinition.Annotations"/> as appropriate.
        /// </summary>
        /// <param name="rule">The rule definition to annotate.</param>
        /// <param name="expression">The validation expression associated with the rule.</param>
        void Annotate(DryvRuleDefinition rule, LambdaExpression expression);
    }
}