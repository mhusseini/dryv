using Dryv.AspNetCore.DynamicControllers.Endpoints;
using Dryv.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dryv.AspNetCore
{
    public static class DryvEndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapDryvEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var dataSource = endpoints.ServiceProvider.GetRequiredService<DryvDynamicEndpointDataSource>();
            endpoints.DataSources.Add(dataSource);

            foreach (var initializer in endpoints.ServiceProvider.GetServices<DryvMvcInitializer>())
            {
                initializer.Run(endpoints.ServiceProvider);
            }

            return endpoints;
        }
    }
}
