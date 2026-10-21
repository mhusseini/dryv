using System.Linq.Expressions;
using Dryv.Rules;

namespace Dryv.Translation
{
    /// <summary>
    /// High-level translation entry point that converts a rule expression
    /// into a <see cref="TranslationResult"/> containing the generated code factory and metadata.
    /// </summary>
    public interface IRuleTranslator
    {
        /// <summary>
        /// Translates the specified expression into a high-level translation result
        /// containing the generated code factory and metadata.
        /// </summary>
        /// <param name="expression">The expression to translate.</param>
        /// <param name="propertyExpression">The member expression identifying the target property, or <c>null</c>.</param>
        /// <param name="rule">The rule definition that owns the expression.</param>
        /// <returns>A <see cref="TranslationResult"/> containing the generated code factory and metadata.</returns>
        TranslationResult Translate(Expression expression, MemberExpression propertyExpression, DryvRuleDefinition rule);
    }
}
