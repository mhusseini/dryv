using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dryv.AspNetCore.DynamicControllers
{
    public class DryvDynamicControllerOptions
    {
        public Type DynamicControllerCallWriterType { get; set; }

        internal Func<DryvControllerGenerationContext, DryvDynamicControllerMethods> GetHttpMethod { get; set; }
        internal Func<DryvControllerGenerationContext, string> GetRoute { get; set; }
        internal Action<DryvControllerGenerationContext, RouteEndpointBuilder> EndpointConfiguration { get; set; }
        internal Func<DryvControllerGenerationContext, RequestDelegate, RequestDelegate> HandlerWrapper { get; set; }
    }
}