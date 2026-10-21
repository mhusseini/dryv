using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.AspNetCore.Tests
{
    [TestClass]
    public class DryvServiceResolverTests
    {
        [TestMethod]
        public void FromFunc_ResolvesService()
        {
            var expected = new object();
            var resolver = DryvServiceResolverFactory.FromFunc(t => expected);

            var result = resolver.GetService(typeof(object));

            Assert.AreSame(expected, result);
        }

        [TestMethod]
        public void FromFunc_ReturnsNullForUnknownType()
        {
            var resolver = DryvServiceResolverFactory.FromFunc(t => null);

            var result = resolver.GetService(typeof(string));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void FromFunc_ThrowsOnNullDelegate()
        {
            var thrown = false;
            try { DryvServiceResolverFactory.FromFunc(null); }
            catch (ArgumentNullException) { thrown = true; }
            Assert.IsTrue(thrown, "Expected ArgumentNullException for null delegate.");
        }

        [TestMethod]
        public void AsDryvServiceResolver_WrapsIServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton("test-value");
            var provider = services.BuildServiceProvider();

            var resolver = provider.AsDryvServiceResolver();
            var result = resolver.GetService(typeof(string));

            Assert.AreEqual("test-value", result);
        }

        [TestMethod]
        public void AsDryvServiceResolver_ReturnsNullForUnregistered()
        {
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();

            var resolver = provider.AsDryvServiceResolver();
            var result = resolver.GetService(typeof(string));

            Assert.IsNull(result);
        }

        [TestMethod]
        public void AsDryvServiceResolver_ThrowsOnNullProvider()
        {
            var thrown = false;
            try { ((IServiceProvider)null).AsDryvServiceResolver(); }
            catch (ArgumentNullException) { thrown = true; }
            Assert.IsTrue(thrown, "Expected ArgumentNullException for null provider.");
        }
    }
}
