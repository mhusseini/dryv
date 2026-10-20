using System;
using Dryv.Configuration;

namespace Dryv.Rules
{
    /// <summary>
    /// Holds the output of translating a validation rule to a target language (e.g. JavaScript).
    /// Composed onto <see cref="DryvCompiledRule"/> as a nullable property so that
    /// translation state is either completely present or absent — never ambiguous.
    /// </summary>
    public class TranslationOutput
    {
        /// <summary>
        /// Gets the code template string produced by the translator, before service injection.
        /// </summary>
        public string CodeTemplate { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the translated expression uses asynchronous operations
        /// (e.g. promise chains in JavaScript).
        /// </summary>
        public bool IsAsync { get; internal set; }

        /// <summary>
        /// Gets the factory delegate that, given a service resolver, service arguments, and options,
        /// produces the final client-side validation code string.
        /// </summary>
        public Func<Func<Type, object>, object[], DryvOptions, string> TranslatedValidationExpression { get; internal set; }

        /// <summary>
        /// Gets the exception that occurred during translation, if any.
        /// When set, <see cref="TranslatedValidationExpression"/> is typically <c>null</c>.
        /// </summary>
        public Exception TranslationError { get; internal set; }
    }
}
