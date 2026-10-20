using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Dryv.AspNetCore.DynamicControllers.Endpoints
{
    internal class DryvDynamicEndpointDataSource : EndpointDataSource
    {
        private readonly object @lock = new();
        private readonly List<Endpoint> endpoints = [];
        private CancellationTokenSource cts = new();

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                lock (@lock)
                {
                    return endpoints.ToArray();
                }
            }
        }

        public override IChangeToken GetChangeToken()
        {
            return new CancellationChangeToken(cts.Token);
        }

        public void AddEndpoint(string routePattern, RequestDelegate handler, string httpMethod, Action<RouteEndpointBuilder> configure = null)
        {
            var builder = new RouteEndpointBuilder(handler, RoutePatternFactory.Parse(routePattern), order: 0)
            {
                DisplayName = $"Dryv: {routePattern}"
            };
            builder.Metadata.Add(new HttpMethodMetadata([httpMethod]));

            configure?.Invoke(builder);

            lock (@lock)
            {
                endpoints.Add(builder.Build());
            }

            var oldCts = cts;
            cts = new CancellationTokenSource();
            oldCts.Cancel();
        }
    }
}
