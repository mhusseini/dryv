namespace Dryv.Extensions
{
    public static class FormattingExtensions
    {
        public static string ToFormattedString(this object value) => value?.ToString() ?? string.Empty;
    }
}