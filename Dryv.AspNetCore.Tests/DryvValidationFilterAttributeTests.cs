using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.AspNetCore.Tests
{
    [TestClass]
    public class DryvValidationFilterAttributeTests
    {
        [TestMethod]
        public async Task OnActionExecutionAsync_CallsNext_WhenDisableAttributePresent()
        {
            var nextCalled = false;
            var filter = new DryvValidationFilterAttribute();

            var context = CreateContext(filters: new List<IFilterMetadata> { new DryvDisableAttribute() });

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
            });

            Assert.IsTrue(nextCalled, "Next delegate should be called when DryvDisableAttribute is present.");
        }

        [TestMethod]
        public async Task OnActionExecutionAsync_CallsNext_WhenNoModel()
        {
            var nextCalled = false;
            var filter = new DryvValidationFilterAttribute();

            var context = CreateContext();

            await filter.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
            });

            Assert.IsTrue(nextCalled, "Next delegate should be called when there is no model.");
        }

        [TestMethod]
        public void Attribute_ParametersProviderType_DefaultsToNull()
        {
            var filter = new DryvValidationFilterAttribute();
            Assert.IsNull(filter.ParametersProviderType);
        }

        [TestMethod]
        public void Attribute_ParametersProviderType_CanBeSet()
        {
            var filter = new DryvValidationFilterAttribute
            {
                ParametersProviderType = typeof(string)
            };

            Assert.AreEqual(typeof(string), filter.ParametersProviderType);
        }

        private static ActionExecutingContext CreateContext(
            Dictionary<string, object> actionArguments = null,
            List<IFilterMetadata> filters = null)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection().BuildServiceProvider()
            };

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary());

            var controller = new TestController();

            return new ActionExecutingContext(
                actionContext,
                filters ?? new List<IFilterMetadata>(),
                actionArguments ?? new Dictionary<string, object>(),
                controller);
        }

        private class TestController : Controller
        {
        }
    }
}
