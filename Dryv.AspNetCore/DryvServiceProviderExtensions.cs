using System;
using Dryv.AspNetCore.Internal;

namespace Dryv.AspNetCore
{
    public static class DryvServiceProviderExtensions
    {
        /// <summary>
        /// Wraps an <see cref="IServiceProvider"/> as an <see cref="IDryvServiceResolver"/>.
        /// </summary>
        public static IDryvServiceResolver AsDryvServiceResolver(this IServiceProvider serviceProvider)
            => new ServiceProviderResolver(serviceProvider);
    }
}
