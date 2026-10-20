using System;
using System.Collections.Generic;
using System.Linq;
using Dryv.AspNetCore;
using Dryv.Configuration;
using Dryv.RuleDetection;
using Dryv.Translation;
using Dryv.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dryv.AspNetCore.Tests
{
    [TestClass]
    public class DryvConfigurationExtensionsTests
    {
        [TestMethod]
        public void RegisterDryv_RegistersDryvOptions()
        {
            var services = new ServiceCollection();
            services.AddControllers().AddDryv();

            var provider = services.BuildServiceProvider();
            var options = provider.GetService<DryvOptions>();

            Assert.IsNotNull(options, "DryvOptions should be registered.");
        }

        [TestMethod]
        public void RegisterDryv_RegistersDryvValidator()
        {
            var services = new ServiceCollection();
            services.AddControllers().AddDryv();

            var provider = services.BuildServiceProvider();
            var validator = provider.GetService<DryvValidator>();

            Assert.IsNotNull(validator, "DryvValidator should be registered.");
        }

        [TestMethod]
        public void RegisterDryv_RegistersDryvTranslator()
        {
            var services = new ServiceCollection();
            services.AddControllers().AddDryv();

            var provider = services.BuildServiceProvider();
            var translator = provider.GetService<DryvTranslator>();

            Assert.IsNotNull(translator, "DryvTranslator should be registered.");
        }

        [TestMethod]
        public void RegisterDryv_RegistersDryvClientWriter()
        {
            var services = new ServiceCollection();
            services.AddControllers().AddDryv();

            var provider = services.BuildServiceProvider();
            var writer = provider.GetService<DryvClientWriter>();

            Assert.IsNotNull(writer, "DryvClientWriter should be registered.");
        }

        [TestMethod]
        public void RegisterDryv_RegistersDefaultTranslators()
        {
            var services = new ServiceCollection();
            services.AddControllers().AddDryv();

            var provider = services.BuildServiceProvider();
            var translators = provider.GetService<IReadOnlyCollection<IDryvMethodCallTranslator>>();

            Assert.IsNotNull(translators, "Method call translators should be registered.");
            Assert.IsTrue(translators.Count > 0, "At least one method call translator should be registered.");
        }

        [TestMethod]
        public void RegisterDryv_SetupActionIsApplied()
        {
            var services = new ServiceCollection();
            services.AddControllers().AddDryv(opts =>
            {
                opts.DisableAutomaticValidation = true;
                opts.TranslationErrorBehavior = TranslationErrorBehavior.ValidateOnServer;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetService<DryvOptions>();

            Assert.IsTrue(options.DisableAutomaticValidation);
            Assert.AreEqual(TranslationErrorBehavior.ValidateOnServer, options.TranslationErrorBehavior);
        }

        [TestMethod]
        public void RegisterDryv_RegistersSetWriter()
        {
            var services = new ServiceCollection();
            services.AddControllers().AddDryv();

            var provider = services.BuildServiceProvider();
            var setWriter = provider.GetService<IDryvClientValidationSetWriter>();

            Assert.IsNotNull(setWriter, "IDryvClientValidationSetWriter should be registered.");
        }

        [TestMethod]
        public void RegisterDryv_RegistersFunctionWriter()
        {
            var services = new ServiceCollection();
            services.AddControllers().AddDryv();

            var provider = services.BuildServiceProvider();
            var funcWriter = provider.GetService<IDryvClientValidationFunctionWriter>();

            Assert.IsNotNull(funcWriter, "IDryvClientValidationFunctionWriter should be registered.");
        }
    }
}
