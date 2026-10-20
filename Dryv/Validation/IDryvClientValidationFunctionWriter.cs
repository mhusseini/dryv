using System;
using System.Collections.Generic;
using System.IO;

namespace Dryv.Validation
{
    /// <summary>
    /// Produces a write action that emits the client-side validation function
    /// for a set of translated rules.
    /// </summary>
    public interface IDryvClientValidationFunctionWriter
    {
        /// <summary>
        /// Creates an <see cref="Action{TextWriter}"/> that, when invoked,
        /// writes the combined client-side validation function for the specified
        /// <paramref name="translatedRules"/> to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="translatedRules">The translated rules to combine into a single validation function.</param>
        /// <returns>A write action that emits the function body.</returns>
        Action<TextWriter> GetValidationFunction(IEnumerable<TranslatedRule> translatedRules);
    }
}