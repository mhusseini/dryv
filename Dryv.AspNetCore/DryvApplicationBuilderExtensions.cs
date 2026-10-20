using Dryv.AspNetCore.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Dryv.AspNetCore
{
    public static class DryvApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDryv(this IApplicationBuilder app)
        {
            foreach (var initializer in app.ApplicationServices.GetServices<DryvMvcInitializer>())
            {
                initializer.Run(app.ApplicationServices);
            }

            return app;
        }
    }
}