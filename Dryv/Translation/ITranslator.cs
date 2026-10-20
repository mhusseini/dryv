using System.IO;
using System.Linq.Expressions;
using Dryv.Rules;

namespace Dryv.Translation
{
    /// <summary>
    /// Defines objects that are able to translate any given <see cref="Expression"/> to the client language.
    /// </summary>
    public interface ITranslator
    {
        /// <summary>
        /// Formats a C# identifier (e.g. property or parameter name) for the target language
        /// (typically camelCase for JavaScript).
        /// </summary>
        /// <param name="name">The original identifier name.</param>
        /// <returns>The formatted identifier.</returns>
        string FormatIdentifier(string name);

        /// <summary>
        /// Translates the specified expression into a high-level translation result
        /// containing the generated code factory and metadata.
        /// </summary>
        /// <param name="expression">The expression to translate.</param>
        /// <param name="propertyExpression">The member expression identifying the target property, or <c>null</c>.</param>
        /// <param name="rule">The rule definition that owns the expression.</param>
        /// <returns>A <see cref="TranslationResult"/> containing the generated code factory and metadata.</returns>
        TranslationResult Translate(Expression expression, MemberExpression propertyExpression, DryvRuleDefinition rule);

        /// <summary>
        /// Translates the specified expression as part of an ongoing translation session,
        /// writing the output into the provided <paramref name="context"/>.
        /// </summary>
        /// <param name="expression">The expression to translate.</param>
        /// <param name="context">The current translation context that carries the writer, model type, and session state.</param>
        /// <param name="negated">If <c>true</c>, the expression should be translated in its logically negated form.</param>
        void Translate(Expression expression, TranslationContext context, bool negated = false);

        /// <summary>
        /// Converts a CLR value to its target-language literal representation
        /// (e.g. a C# <see cref="string"/> to a JavaScript string literal).
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The literal representation in the target language.</returns>
        string TranslateValue(object value);

        /// <summary>
        /// Attempts to write a terminal (leaf) expression directly to the <paramref name="writer"/>
        /// without full visitor dispatch.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <param name="writer">The writer to emit the result into.</param>
        /// <returns><c>true</c> if the expression was handled; otherwise, <c>false</c>.</returns>
        bool TryWriteTerminal(Expression expression, TextWriter writer);
    }
}