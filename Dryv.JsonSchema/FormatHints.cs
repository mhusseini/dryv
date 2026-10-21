using System.Text.RegularExpressions;

namespace Dryv.JsonSchema;

/// <summary>
/// Detects well-known regex patterns and maps them to JSON Schema
/// <c>format</c> keywords (e.g. "email", "uri", "date").
/// </summary>
internal static class FormatHints
{
    private static readonly (Regex Pattern, string Format)[] KnownFormats =
    [
        // Common email patterns
        (new Regex(@"^\^?\[?\^?@.*@", RegexOptions.Compiled), "email"),

        // URI/URL patterns
        (new Regex(@"https?://", RegexOptions.Compiled | RegexOptions.IgnoreCase), "uri"),

        // Date patterns (ISO 8601)
        (new Regex(@"\\d\{4\}[\\-]\\d\{2\}[\\-]\\d\{2\}", RegexOptions.Compiled), "date"),

        // IPv4 patterns
        (new Regex(@"\\d\{1,3\}\\\.\\d\{1,3\}\\\.\\d\{1,3\}\\\.\\d\{1,3\}", RegexOptions.Compiled), "ipv4"),
    ];

    /// <summary>
    /// If the given regex pattern looks like a well-known format, adds a
    /// <c>format</c> entry to the constraints dictionary.
    /// </summary>
    internal static void TryApplyFormatHint(string regexPattern, Dictionary<string, object?> constraints)
    {
        foreach (var (pattern, format) in KnownFormats)
        {
            if (!pattern.IsMatch(regexPattern))
            {
                continue;
            }
            
            constraints["format"] = format;
            return;
        }
    }
}
