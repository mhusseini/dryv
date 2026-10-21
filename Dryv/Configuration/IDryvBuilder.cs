namespace Dryv.Configuration
{
    /// <summary>
    /// An interface for configuring Dryv services.
    /// </summary>
    public interface IDryvBuilder<out TBuilder> where TBuilder : IDryvBuilder<TBuilder>
    {
        /// <summary>
        /// Gets the <see cref="DryvOptions"/> that this builder configures.
        /// </summary>
        DryvOptions Options { get; }
    }
}