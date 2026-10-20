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
        private static T Resolve<T>(Func<Type, object> r) => (T)r(typeof(T));

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
        /// <param name="registerScopedFactory">
        /// Registers a scoped factory that produces a service given a service resolver function.
        /// Maps to <c>AddScoped(serviceType, sp =&gt; factory(sp.GetService))</c> in DI frameworks.
        /// </param>
        public static void RegisterServices(
            DryvOptions options,
            Action<Type, Type> registerSingleton,
            Action<Type, object> registerInstance,
            Action<Type, Func<Func<Type, object>, object>> registerFactory,
            Action<Type, Func<Func<Type, object>, object>> registerScopedFactory)
        {
            // Interface → implementation bindings
            registerSingleton(typeof(IDryvClientValidationFunctionWriter), options.ClientFunctionWriterType ?? DryvOptions.DefaultClientFunctionWriterType);
            registerSingleton(typeof(IDryvClientValidationSetWriter), options.ClientValidationSetWriterType ?? DryvOptions.DefaultClientValidationSetWriterType);

            var translatorType = options.TranslatorType ?? typeof(JavaScriptTranslator);
            registerSingleton(typeof(ITranslator), translatorType);
            registerSingleton(typeof(IRuleTranslator), translatorType);
            registerSingleton(typeof(IExpressionWriter), translatorType);
            registerSingleton(typeof(IValueFormatter), translatorType);

            // Core services (factory-based to allow internal constructors)
            registerFactory(typeof(ModelTreeBuilder), _ => new ModelTreeBuilder());
            registerFactory(typeof(DryvCompiler), _ => new DryvCompiler());
            registerFactory(typeof(RuleDiscovery), _ => new RuleDiscovery());
            registerFactory(typeof(DryvRuleFinder), r => new DryvRuleFinder(
                Resolve<ModelTreeBuilder>(r),
                Resolve<DryvCompiler>(r),
                Resolve<RuleDiscovery>(r),
                Resolve<IReadOnlyCollection<IDryvRuleAnnotator>>(r)));
            registerFactory(typeof(DryvValidationCache), r => new DryvValidationCache(
                Resolve<DryvRuleFinder>(r)));
            registerFactory(typeof(RuleTranslationPhase), r => new RuleTranslationPhase(
                Resolve<DryvRuleFinder>(r),
                Resolve<IRuleTranslator>(r),
                Resolve<DryvOptions>(r)));
            registerFactory(typeof(RuleResolutionPipeline), _ => new RuleResolutionPipeline());
            registerFactory(typeof(DryvTranslator), r => new DryvTranslator(
                Resolve<RuleTranslationPhase>(r),
                Resolve<RuleResolutionPipeline>(r)));

            // Scoped core services
            registerScopedFactory(typeof(DryvValidator), r => new DryvValidator(
                Resolve<DryvValidationCache>(r),
                Resolve<IDryvServiceResolver>(r),
                Resolve<DryvOptions>(r),
                Resolve<RuleResolutionPipeline>(r)));

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
        private static IReadOnlyCollection<IDryvRuleAnnotator> BuildAnnotators(
            DryvOptions options, Func<Type, object> resolver)
            => new ReadOnlyCollection<IDryvRuleAnnotator>(
                options.Annotators
                    .Select(resolver)
                    .Cast<IDryvRuleAnnotator>()
                    .ToList());

        /// <summary>
        /// Builds the method-call translator collection from the configured options.
        /// </summary>
        private static IReadOnlyCollection<IDryvMethodCallTranslator> BuildMethodCallTranslators(
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
        private static IReadOnlyCollection<IDryvCustomTranslator> BuildCustomTranslators(
            DryvOptions options, Func<Type, object> resolver)
            => new ReadOnlyCollection<IDryvCustomTranslator>(
                options.Translators
                    .Where(t => typeof(IDryvCustomTranslator).IsAssignableFrom(t))
                    .Select(resolver)
                    .Cast<IDryvCustomTranslator>()
                    .ToList());
    }
}
