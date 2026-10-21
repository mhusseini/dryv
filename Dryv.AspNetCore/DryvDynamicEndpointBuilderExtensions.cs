using System;
using Dryv.AspNetCore.DynamicControllers;
using Dryv.AspNetCore.DynamicControllers.Endpoints;
using Dryv.AspNetCore.DynamicControllers.Translation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Dryv.AspNetCore
{
    public static class DryvDynamicEndpointBuilderExtensions
    {
        public static IDryvMvcBuilder AddDryvDynamicEndpoints(this IDryvMvcBuilder dryvBuilder, Action<DryvDynamicControllerOptions> setupAction = null)
        {
            dryvBuilder.Options.Translators.Add<DryvDynamicControllerTranslator>();
            dryvBuilder.Options.Translators.Add<DryvDynamicControllerMethodTranslator>();

            var options = new DryvDynamicControllerOptions();
            setupAction?.Invoke(options);

            options.GetHttpMethod ??= CustomizationDefaults.DefaultHttpMethod;
            options.GetRoute ??= CustomizationDefaults.DefaultRoute;

            var services = dryvBuilder.MvcBuilder.Services;
            services.AddSingleton(Options.Create(options));
            services.AddSingleton<DryvDynamicEndpointDataSource>();
            services.AddSingleton<DryvDynamicEndpointRegistration>();
            services.TryAddSingleton(typeof(IDryvClientServerCallWriter), options.DynamicControllerCallWriterType ?? typeof(DryvClientServerCallWriter));

            return dryvBuilder;
        }
    }
}
