using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dryv.AspNetCore.DynamicControllers.Endpoints;
using Dryv.AspNetCore.Internal;
using Dryv.AspNetCore.Json;
using Dryv.AspNetCore.Translators;
using Dryv.Configuration;
using Dryv.RuleDetection;
using Dryv.Rules;
using Dryv.Translation;
using Dryv.Translation.Translators;
using Dryv.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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

        private static DryvMvcCoreOptions RegisterDryv(
            IServiceCollection serviceCollection,
            Action<DryvOptions> setupAction,
            Action<JsonSerializerOptions> jsonSetupAction)
        {
            var options = new DryvMvcCoreOptions(t => serviceCollection.AddSingleton(t, t));

            options.Translators.Add<DryvValidationResultTranslator>();
            options.Translators.Add<DateTimeTranslator>();
            options.Translators.Add<StringTranslator>();
            options.Translators.Add<ToStringTranslator>();
            options.Translators.Add<EnumerableTranslator>();
            options.Translators.Add<RegexTranslator>();
            options.Translators.Add<DryvParametersTranslator>();
            options.Translators.Add<CustomCodeTranslator>();
            options.Translators.Add<FormFileCollectionTranslator>();
            options.Translators.Add<FormFileTranslator>();
            options.Translators.Add<FormattingExtensionsTranslator>();

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

            serviceCollection.TryAddSingleton<DryvEndpointRouteBuilderProvider>();
            serviceCollection.TryAddSingleton(typeof(IDryvClientValidationFunctionWriter), options.ClientFunctionWriterType ?? DryvOptions.DefaultClientFunctionWriterType);
            serviceCollection.TryAddSingleton(typeof(IDryvClientValidationSetWriter), options.ClientValidationSetWriterType ?? DryvOptions.DefaultClientValidationSetWriterType);
            serviceCollection.TryAddSingleton<ITranslator, JavaScriptTranslator>();
            serviceCollection.TryAddSingleton<ModelTreeBuilder>();
            serviceCollection.TryAddSingleton<DryvValidator>();
            serviceCollection.TryAddSingleton<DryvCompiler>();
            serviceCollection.TryAddSingleton<DryvTranslator>();
            serviceCollection.TryAddSingleton<DryvRuleFinder>();
            serviceCollection.TryAddSingleton<DryvClientWriter>();
            serviceCollection.AddSingleton(Options.Create((DryvOptions)options));
            serviceCollection.AddSingleton(typeof(DryvOptions), options);

            serviceCollection.AddSingleton<IReadOnlyCollection<IDryvRuleAnnotator>>(services => new ReadOnlyCollection<IDryvRuleAnnotator>(options.Annotators.Select(services.GetService).Cast<IDryvRuleAnnotator>().ToList()));
            serviceCollection.AddSingleton<IReadOnlyCollection<IDryvMethodCallTranslator>>(services => new ReadOnlyCollection<IDryvMethodCallTranslator>(options.Translators.Where(t => typeof(IDryvMethodCallTranslator).IsAssignableFrom(t)).Select(services.GetService).Cast<IDryvMethodCallTranslator>().ToList()));
            serviceCollection.AddSingleton<IReadOnlyCollection<IDryvCustomTranslator>>(services => new ReadOnlyCollection<IDryvCustomTranslator>(options.Translators.Where(t => typeof(IDryvCustomTranslator).IsAssignableFrom(t)).Select(services.GetService).Cast<IDryvCustomTranslator>().ToList()));
            return options;
        }
    }
}