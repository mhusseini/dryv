using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dryv.AspNetCore.Internal
{
    /// <summary>
    /// Wraps an <see cref="IServiceProvider"/> as an <see cref="IDryvServiceResolver"/>,
    /// using <see cref="ServiceProviderServiceExtensions.GetRequiredService"/> for
    /// better error messages when a service is not registered.
    /// </summary>
    internal sealed class ServiceProviderResolver(IServiceProvider serviceProvider) : IDryvServiceResolver
    {
        private readonly IServiceProvider serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        public object GetService(Type serviceType) => this.serviceProvider.GetService(serviceType);
    }
}
