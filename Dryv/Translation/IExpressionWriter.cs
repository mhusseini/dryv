using System.IO;
using System.Linq.Expressions;

namespace Dryv.Translation
{
    /// <summary>
    /// Low-level expression visitor that writes translated output into a
    /// <see cref="TranslationContext"/> during an ongoing translation session.
    /// </summary>
    public interface IExpressionWriter
    {
        /// <summary>
        /// Translates the specified expression as part of an ongoing translation session,
        /// writing the output into the provided <paramref name="context"/>.
        /// </summary>
        /// <param name="expression">The expression to translate.</param>
        /// <param name="context">The current translation context that carries the writer, model type, and session state.</param>
        /// <param name="negated">If <c>true</c>, the expression should be translated in its logically negated form.</param>
        void Translate(Expression expression, TranslationContext context, bool negated = false);

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
