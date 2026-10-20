using Dryv.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dryv.AspNetCore
{
    /// <summary>
    /// Extends <see cref="IDryvBuilder{TBuilder}"/> with access to the ASP.NET Core
    /// <see cref="IMvcBuilder"/>, allowing Dryv configuration to also configure MVC services.
    /// </summary>
    public interface IDryvMvcBuilder : IDryvBuilder<IDryvMvcBuilder>
    {
        /// <summary>
        /// Gets the underlying <see cref="IMvcBuilder"/> so that MVC-specific services
        /// can be registered alongside Dryv services.
        /// </summary>
        IMvcBuilder MvcBuilder { get; }
    }
}