namespace Dryv.Translation
{
    /// <summary>
    /// Combines <see cref="IRuleTranslator"/>, <see cref="IExpressionWriter"/>,
    /// and <see cref="IValueFormatter"/> into a single composite interface for
    /// backward compatibility.  New consumers should depend on the narrowest
    /// sub-interface they actually need.
    /// </summary>
    public interface ITranslator : IRuleTranslator, IExpressionWriter, IValueFormatter
    {
    }
}