namespace Dryv.Translation
{
    /// <summary>
    /// Provides identifier and value formatting for a target language
    /// (e.g. camelCase identifiers and JavaScript literals).
    /// </summary>
    public interface IValueFormatter
    {
        /// <summary>
        /// Formats a C# identifier (e.g. property or parameter name) for the target language
        /// (typically camelCase for JavaScript).
        /// </summary>
        /// <param name="name">The original identifier name.</param>
        /// <returns>The formatted identifier.</returns>
        string FormatIdentifier(string name);

        /// <summary>
        /// Converts a CLR value to its target-language literal representation
        /// (e.g. a C# <see cref="string"/> to a JavaScript string literal).
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The literal representation in the target language.</returns>
        string TranslateValue(object value);
    }
}
