using System;
using System.Collections.Generic;
using System.IO;

namespace Dryv.Validation
{
    /// <summary>
    /// Writes a complete client-side validation set (begin marker, per-model validation
    /// entries, and end marker) to a <see cref="TextWriter"/>.
    /// </summary>
    public interface IDryvClientValidationSetWriter
    {
        /// <summary>
        /// Writes the opening fragment that precedes all validation sets
        /// (e.g. the opening of a script tag or module wrapper).
        /// </summary>
        /// <param name="writer">The target writer.</param>
        void WriteBegin(TextWriter writer);

        /// <summary>
        /// Writes the closing fragment that follows all validation sets
        /// (e.g. the closing of a script tag or module wrapper).
        /// </summary>
        /// <param name="writer">The target writer.</param>
        void WriteEnd(TextWriter writer);

        /// <summary>
        /// Writes a single validation set containing the specified validators,
        /// disablers and parameters for a given model.
        /// </summary>
        /// <param name="writer">The target writer.</param>
        /// <param name="validationSetName">A unique name identifying the model this set belongs to.</param>
        /// <param name="validators">A dictionary mapping property paths to their validation function writers.</param>
        /// <param name="disablers">A dictionary mapping property paths to their disabling function writers.</param>
        /// <param name="parameters">A dictionary of named parameter values to embed alongside the functions.</param>
        void WriteValidationSet(TextWriter writer, string validationSetName, IDictionary<string, Action<TextWriter>> validators, IDictionary<string, Action<TextWriter>> disablers, IDictionary<string, object> parameters);
    }
}