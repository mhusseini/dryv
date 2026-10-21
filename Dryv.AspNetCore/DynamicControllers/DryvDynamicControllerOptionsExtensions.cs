using System;
using Dryv.AspNetCore.DynamicControllers.Translation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dryv.AspNetCore.DynamicControllers
{
    public static class DryvDynamicControllerOptionsExtensions
    {
        public static void UseServerCallWriter<T>(this DryvDynamicControllerOptions options) where T : IDryvClientServerCallWriter
        {
            options.DynamicControllerCallWriterType = typeof(T);
        }

        public static void WithRoute(this DryvDynamicControllerOptions options, Func<DryvControllerGenerationContext, string> template)
        {
            options.GetRoute = template;
        }

        /// <summary>
        /// Configures the endpoint builder for each dynamically generated endpoint.
        /// Use this to add metadata such as authorization policies, CORS, rate limiting, etc.
        /// </summary>
        public static void ConfigureEndpoint(this DryvDynamicControllerOptions options, Action<DryvControllerGenerationContext, RouteEndpointBuilder> configure)
        {
            options.EndpointConfiguration = configure;
        }

        /// <summary>
        /// Wraps the request handler for each dynamically generated endpoint.
        /// Use this to add custom request/response logic, equivalent to MVC action filters.
        /// </summary>
        public static void WrapHandler(this DryvDynamicControllerOptions options, Func<DryvControllerGenerationContext, RequestDelegate, RequestDelegate> wrapper)
        {
            options.HandlerWrapper = wrapper;
        }
    }
}