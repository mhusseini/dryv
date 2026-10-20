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
        private readonly object _lock = new object();
        private readonly List<Endpoint> _endpoints = new List<Endpoint>();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                lock (_lock)
                {
                    return _endpoints.ToArray();
                }
            }
        }

        public override IChangeToken GetChangeToken()
        {
            return new CancellationChangeToken(_cts.Token);
        }

        public void AddEndpoint(string routePattern, RequestDelegate handler, string httpMethod, Action<RouteEndpointBuilder> configure = null)
        {
            var builder = new RouteEndpointBuilder(handler, RoutePatternFactory.Parse(routePattern), order: 0)
            {
                DisplayName = $"Dryv: {routePattern}"
            };
            builder.Metadata.Add(new HttpMethodMetadata(new[] { httpMethod }));

            configure?.Invoke(builder);

            lock (_lock)
            {
                _endpoints.Add(builder.Build());
            }

            var oldCts = _cts;
            _cts = new CancellationTokenSource();
            oldCts.Cancel();
        }
    }
}
