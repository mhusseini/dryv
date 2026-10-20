using System.Threading.Tasks;
using Dryv.AspNetCore.DynamicControllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.AspNetCore.Tests
{
    [TestClass]
    public class DryvDynamicControllerOptionsTests
    {
        [TestMethod]
        public void ConfigureEndpoint_SetsEndpointConfiguration()
        {
            var options = new DryvDynamicControllerOptions();

            options.ConfigureEndpoint((ctx, endpoint) =>
            {
                endpoint.Metadata.Add(new EndpointNameMetadata("test"));
            });

            Assert.IsNotNull(options.EndpointConfiguration);
        }

        [TestMethod]
        public void ConfigureEndpoint_CallbackReceivesContextAndBuilder()
        {
            var options = new DryvDynamicControllerOptions();
            DryvControllerGenerationContext receivedContext = null;
            RouteEndpointBuilder receivedBuilder = null;

            options.ConfigureEndpoint((ctx, builder) =>
            {
                receivedContext = ctx;
                receivedBuilder = builder;
            });

            var context = new DryvControllerGenerationContext { Action = "TestAction" };
            var endpointBuilder = new RouteEndpointBuilder(
                ctx => Task.CompletedTask,
                RoutePatternFactory.Parse("/_v/test"),
                order: 0);

            options.EndpointConfiguration(context, endpointBuilder);

            Assert.AreSame(context, receivedContext);
            Assert.AreSame(endpointBuilder, receivedBuilder);
        }

        [TestMethod]
        public void ConfigureEndpoint_CanAddMetadata()
        {
            var options = new DryvDynamicControllerOptions();

            options.ConfigureEndpoint((ctx, builder) =>
            {
                builder.Metadata.Add(new EndpointNameMetadata($"Dryv:{ctx.Action}"));
            });

            var context = new DryvControllerGenerationContext { Action = "ValidateEmail" };
            var endpointBuilder = new RouteEndpointBuilder(
                ctx => Task.CompletedTask,
                RoutePatternFactory.Parse("/_v/test"),
                order: 0);

            options.EndpointConfiguration(context, endpointBuilder);

            var endpoint = endpointBuilder.Build();
            var nameMetadata = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
            Assert.IsNotNull(nameMetadata);
            Assert.AreEqual("Dryv:ValidateEmail", nameMetadata.EndpointName);
        }

        [TestMethod]
        public void WrapHandler_SetsHandlerWrapper()
        {
            var options = new DryvDynamicControllerOptions();

            options.WrapHandler((ctx, next) => next);

            Assert.IsNotNull(options.HandlerWrapper);
        }

        [TestMethod]
        public void WrapHandler_CanWrapRequestDelegate()
        {
            var options = new DryvDynamicControllerOptions();
            var beforeCalled = false;
            var afterCalled = false;
            var innerCalled = false;

            options.WrapHandler((ctx, next) => async httpContext =>
            {
                beforeCalled = true;
                await next(httpContext);
                afterCalled = true;
            });

            var context = new DryvControllerGenerationContext { Action = "Test" };
            RequestDelegate inner = ctx =>
            {
                innerCalled = true;
                return Task.CompletedTask;
            };

            var wrapped = options.HandlerWrapper(context, inner);
            wrapped(new DefaultHttpContext()).GetAwaiter().GetResult();

            Assert.IsTrue(beforeCalled, "Before logic should have been called");
            Assert.IsTrue(innerCalled, "Inner handler should have been called");
            Assert.IsTrue(afterCalled, "After logic should have been called");
        }

        [TestMethod]
        public void WrapHandler_CanShortCircuit()
        {
            var options = new DryvDynamicControllerOptions();
            var innerCalled = false;

            options.WrapHandler((ctx, next) => async httpContext =>
            {
                httpContext.Response.StatusCode = 403;
                // intentionally not calling next
            });

            var context = new DryvControllerGenerationContext { Action = "Test" };
            RequestDelegate inner = ctx =>
            {
                innerCalled = true;
                return Task.CompletedTask;
            };

            var httpContext = new DefaultHttpContext();
            var wrapped = options.HandlerWrapper(context, inner);
            wrapped(httpContext).GetAwaiter().GetResult();

            Assert.IsFalse(innerCalled, "Inner handler should NOT have been called");
            Assert.AreEqual(403, httpContext.Response.StatusCode);
        }

        [TestMethod]
        public void WithRoute_SetsGetRoute()
        {
            var options = new DryvDynamicControllerOptions();

            options.WithRoute(ctx => $"api/validate/{ctx.Action}");

            Assert.IsNotNull(options.GetRoute);

            var context = new DryvControllerGenerationContext { Action = "CheckEmail" };
            var route = options.GetRoute(context);
            Assert.AreEqual("api/validate/CheckEmail", route);
        }

        [TestMethod]
        public void Options_DefaultsAreNull()
        {
            var options = new DryvDynamicControllerOptions();

            Assert.IsNull(options.EndpointConfiguration);
            Assert.IsNull(options.HandlerWrapper);
            Assert.IsNull(options.GetRoute);
            Assert.IsNull(options.GetHttpMethod);
        }
    }
}
