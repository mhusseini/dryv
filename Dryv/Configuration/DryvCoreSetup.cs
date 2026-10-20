using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dryv.RuleDetection;
using Dryv.Rules;
using Dryv.Translation;
using Dryv.Translation.Translators;
using Dryv.Validation;

namespace Dryv.Configuration
{
    /// <summary>
    /// Provides framework-agnostic setup for core Dryv services.
    /// Use this class to configure Dryv without depending on ASP.NET Core.
    /// </summary>
    public static class DryvCoreSetup
    {
        /// <summary>
        /// The core Dryv service types that must be registered as singletons.
        /// </summary>
        public static readonly Type[] SingletonServiceTypes =
        {
            typeof(ModelTreeBuilder),
            typeof(DryvValidationCache),
            typeof(DryvCompiler),
            typeof(RuleDiscovery),
            typeof(DryvRuleFinder),
            typeof(RuleTranslationPhase),
            typeof(DryvTranslator),
        };

        /// <summary>
        /// The core Dryv service types that should be registered as scoped
        /// (or singleton in environments without scoping).
        /// </summary>
        public static readonly Type[] ScopedServiceTypes =
        {
            typeof(DryvValidator),
        };

        /// <summary>
        /// Populates default configuration on the specified options.
        /// Adds the default <see cref="JavaScriptTranslator"/> and core translators
        /// when no custom <see cref="DryvOptions.TranslatorType"/> is configured.
        /// </summary>
        public static void ConfigureDefaults(DryvOptions options)
        {
            if (options.TranslatorType != null)
            {
                return;
            }

            options.Translators.Add<DryvValidationResultTranslator>();
            options.Translators.Add<DateTimeTranslator>();
            options.Translators.Add<StringTranslator>();
            options.Translators.Add<ToStringTranslator>();
            options.Translators.Add<EnumerableTranslator>();
            options.Translators.Add<RegexTranslator>();
            options.Translators.Add<DryvParametersTranslator>();
            options.Translators.Add<CustomCodeTranslator>();
        }

        /// <summary>
        /// Registers core Dryv services using the provided callbacks.
        /// Call this after all translators, annotators, and options have been fully configured.
        /// </summary>
        /// <param name="options">The configured Dryv options.</param>
        /// <param name="registerSingleton">
        /// Registers a service type → implementation type mapping as a singleton.
        /// Maps to <c>TryAddSingleton(serviceType, implementationType)</c> in DI frameworks.
        /// </param>
        /// <param name="registerScoped">
        /// Registers a service type → implementation type mapping as scoped.
        /// In environments without scoping (e.g. a console app), this may map to singleton.
        /// Maps to <c>TryAddScoped(serviceType, implementationType)</c> in DI frameworks.
        /// </param>
        /// <param name="registerInstance">
        /// Registers a pre-built instance for a service type.
        /// Maps to <c>AddSingleton(serviceType, instance)</c> in DI frameworks.
        /// </param>
        /// <param name="registerFactory">
        /// Registers a factory that produces a service given a service resolver function.
        /// The inner <c>Func&lt;Type, object&gt;</c> parameter is the resolver
        /// (e.g. <c>IServiceProvider.GetService</c>).
        /// Maps to <c>AddSingleton(serviceType, sp =&gt; factory(sp.GetService))</c> in DI frameworks.
        /// </param>
        public static void RegisterServices(
            DryvOptions options,
            Action<Type, Type> registerSingleton,
            Action<Type, Type> registerScoped,
            Action<Type, object> registerInstance,
            Action<Type, Func<Func<Type, object>, object>> registerFactory)
        {
            // Interface → implementation bindings
            registerSingleton(typeof(IDryvClientValidationFunctionWriter), options.ClientFunctionWriterType ?? DryvOptions.DefaultClientFunctionWriterType);
            registerSingleton(typeof(IDryvClientValidationSetWriter), options.ClientValidationSetWriterType ?? DryvOptions.DefaultClientValidationSetWriterType);

            registerSingleton(typeof(ITranslator), options.TranslatorType ?? typeof(JavaScriptTranslator));

            // Singleton core services
            foreach (var type in SingletonServiceTypes)
            {
                registerSingleton(type, type);
            }

            // Scoped core services
            foreach (var type in ScopedServiceTypes)
            {
                registerScoped(type, type);
            }

            // Translator and annotator implementation types
            foreach (var type in options.Translators)
            {
                registerSingleton(type, type);
            }

            foreach (var type in options.Annotators)
            {
                registerSingleton(type, type);
            }

            // Options instance
            registerInstance(typeof(DryvOptions), options);

            // Collection factories (resolved lazily — picks up translators/annotators
            // added after this call, as long as they are added before resolution time)
            registerFactory(
                typeof(IReadOnlyCollection<IDryvRuleAnnotator>),
                resolver => BuildAnnotators(options, resolver));

            registerFactory(
                typeof(IReadOnlyCollection<IDryvMethodCallTranslator>),
                resolver => BuildMethodCallTranslators(options, resolver));

            registerFactory(
                typeof(IReadOnlyCollection<IDryvCustomTranslator>),
                resolver => BuildCustomTranslators(options, resolver));
        }

        /// <summary>
        /// Builds the annotator collection from the configured options.
        /// </summary>
        public static IReadOnlyCollection<IDryvRuleAnnotator> BuildAnnotators(
            DryvOptions options, Func<Type, object> resolver)
            => new ReadOnlyCollection<IDryvRuleAnnotator>(
                options.Annotators
                    .Select(resolver)
                    .Cast<IDryvRuleAnnotator>()
                    .ToList());

        /// <summary>
        /// Builds the method-call translator collection from the configured options.
        /// </summary>
        public static IReadOnlyCollection<IDryvMethodCallTranslator> BuildMethodCallTranslators(
            DryvOptions options, Func<Type, object> resolver)
            => new ReadOnlyCollection<IDryvMethodCallTranslator>(
                options.Translators
                    .Where(t => typeof(IDryvMethodCallTranslator).IsAssignableFrom(t))
                    .Select(resolver)
                    .Cast<IDryvMethodCallTranslator>()
                    .ToList());

        /// <summary>
        /// Builds the custom translator collection from the configured options.
        /// </summary>
        public static IReadOnlyCollection<IDryvCustomTranslator> BuildCustomTranslators(
            DryvOptions options, Func<Type, object> resolver)
            => new ReadOnlyCollection<IDryvCustomTranslator>(
                options.Translators
                    .Where(t => typeof(IDryvCustomTranslator).IsAssignableFrom(t))
                    .Select(resolver)
                    .Cast<IDryvCustomTranslator>()
                    .ToList());
    }
}
