using System.Linq;
using System.Threading.Tasks;
using Dryv.AspNetCore.DynamicControllers.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.AspNetCore.Tests
{
    [TestClass]
    public class DryvDynamicEndpointDataSourceTests
    {
        [TestMethod]
        public void AddEndpoint_WithoutConfigure_CreatesEndpointWithHttpMethodMetadata()
        {
            var dataSource = new DryvDynamicEndpointDataSource();
            RequestDelegate handler = ctx => Task.CompletedTask;

            dataSource.AddEndpoint("/_v/test1", handler, "POST");

            var endpoints = dataSource.Endpoints;
            Assert.AreEqual(1, endpoints.Count);

            var endpoint = endpoints[0] as RouteEndpoint;
            Assert.IsNotNull(endpoint);
            Assert.AreEqual("/_v/test1", endpoint.RoutePattern.RawText);

            var httpMethodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            Assert.IsNotNull(httpMethodMetadata);
            CollectionAssert.Contains(httpMethodMetadata.HttpMethods.ToList(), "POST");
        }

        [TestMethod]
        public void AddEndpoint_WithConfigure_InvokesConfigureCallback()
        {
            var dataSource = new DryvDynamicEndpointDataSource();
            RequestDelegate handler = ctx => Task.CompletedTask;
            var configureInvoked = false;

            dataSource.AddEndpoint("/_v/test2", handler, "GET", builder =>
            {
                configureInvoked = true;
            });

            Assert.IsTrue(configureInvoked);
        }

        [TestMethod]
        public void AddEndpoint_WithConfigure_AddsCustomMetadata()
        {
            var dataSource = new DryvDynamicEndpointDataSource();
            RequestDelegate handler = ctx => Task.CompletedTask;

            dataSource.AddEndpoint("/_v/test3", handler, "POST", builder =>
            {
                builder.Metadata.Add(new EndpointNameMetadata("CustomName"));
            });

            var endpoint = dataSource.Endpoints[0];
            var nameMetadata = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
            Assert.IsNotNull(nameMetadata);
            Assert.AreEqual("CustomName", nameMetadata.EndpointName);
        }

        [TestMethod]
        public void AddEndpoint_MultipleEndpoints_AllAccessible()
        {
            var dataSource = new DryvDynamicEndpointDataSource();
            RequestDelegate handler = ctx => Task.CompletedTask;

            dataSource.AddEndpoint("/_v/a", handler, "POST");
            dataSource.AddEndpoint("/_v/b", handler, "GET");
            dataSource.AddEndpoint("/_v/c", handler, "POST");

            Assert.AreEqual(3, dataSource.Endpoints.Count);
        }

        [TestMethod]
        public void AddEndpoint_SetsDisplayName()
        {
            var dataSource = new DryvDynamicEndpointDataSource();
            RequestDelegate handler = ctx => Task.CompletedTask;

            dataSource.AddEndpoint("/_v/myroute", handler, "POST");

            var endpoint = dataSource.Endpoints[0];
            Assert.AreEqual("Dryv: /_v/myroute", endpoint.DisplayName);
        }

        [TestMethod]
        public void GetChangeToken_SignaledAfterAddEndpoint()
        {
            var dataSource = new DryvDynamicEndpointDataSource();
            var token = dataSource.GetChangeToken();

            Assert.IsFalse(token.HasChanged);

            dataSource.AddEndpoint("/_v/x", ctx => Task.CompletedTask, "POST");

            Assert.IsTrue(token.HasChanged);
        }
    }
}
