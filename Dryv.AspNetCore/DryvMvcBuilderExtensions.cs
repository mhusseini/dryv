using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dryv.AspNetCore.Internal;
using Dryv.AspNetCore.Json;
using Dryv.AspNetCore.Translators;
using Dryv.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dryv.AspNetCore
{
    public static class DryvConfigurationExtensions
    {
        public static IDryvMvcBuilder AddDryv(
            this IMvcBuilder mvcBuilder,
            Action<DryvOptions> setupAction = null,
            Action<JsonSerializerOptions> jsonSetupAction = null)
        {
            var options = RegisterDryv(mvcBuilder.Services, setupAction, jsonSetupAction);

            if (!options.DisableAutomaticValidation)
            {
                mvcBuilder.AddMvcOptions(opts => opts.Filters.Add<DryvValidationFilterAttribute>());
            }

            return new DryvMvcBuilder(options, mvcBuilder);
        }

        public static IServiceCollection AddDryv(
            this IServiceCollection serviceCollection,
            Action<DryvOptions> setupAction = null,
            Action<JsonSerializerOptions> jsonSetupAction = null)
        {
            RegisterDryv(serviceCollection, setupAction, jsonSetupAction);
            return serviceCollection;
        }

        private static DryvOptions RegisterDryv(
            IServiceCollection serviceCollection,
            Action<DryvOptions> setupAction,
            Action<JsonSerializerOptions> jsonSetupAction)
        {
            var options = new DryvOptions();

            setupAction?.Invoke(options);

            if (options.JsonConversion == null)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters =
                    {
                        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                        new ValueTupleFactory()
                    },
                };

                jsonSetupAction?.Invoke(jsonOptions);

                options.JsonConversion = v => JsonSerializer.Serialize(v, jsonOptions);
            }

            // Core defaults (adds core translators when no custom translator is set)
            DryvCoreSetup.ConfigureDefaults(options);

            // ASP.NET-specific translators
            if (options.TranslatorType == null)
            {
                options.Translators.Add<FormFileCollectionTranslator>();
                options.Translators.Add<FormFileTranslator>();
            }

            // Register all core services via DryvCoreSetup
            DryvCoreSetup.RegisterServices(
                options,
                registerSingleton: (svc, impl) => serviceCollection.TryAddSingleton(svc, impl),
                registerScoped: (svc, impl) => serviceCollection.TryAddScoped(svc, impl),
                registerInstance: (svc, inst) => serviceCollection.AddSingleton(svc, inst),
                registerFactory: (svc, factory) => serviceCollection.AddSingleton(svc, sp => factory(sp.GetService)));

            // Scoped IDryvServiceResolver wrapping the current scope's IServiceProvider
            serviceCollection.TryAddScoped<IDryvServiceResolver>(sp => new Internal.ServiceProviderResolver(sp));

            // ASP.NET-specific service
            serviceCollection.TryAddSingleton<DryvClientWriter>();

            return options;
        }
    }
}