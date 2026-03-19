using System;
using System.Collections.Generic;
using Dryv.RuleDetection;
using Dryv.Rules;
using Microsoft.Extensions.Options;

namespace Dryv.AspNetCore.PreLoading
{
    internal class DryvPreloader
    {
        private readonly IOptions<DryvPreloadingOptions> options;
        private readonly DryvRuleFinder ruleFinder;
        private readonly DryvTranslator translator;
        private bool hasStarted;

        public DryvPreloader(DryvRuleFinder ruleFinder, DryvTranslator translator, IOptions<DryvPreloadingOptions> options)
        {
            this.ruleFinder = ruleFinder;
            this.translator = translator;
            this.options = options;
        }

        public void Preload(Func<Type, object> serviceProvider = null)
        {
            if (this.hasStarted || !this.options.Value.IsEnabled)
            {
                return;
            }

            this.hasStarted = true;

            var sets = DryvSets.GetDryvSets();

            foreach (var (type, _) in sets)
            {
                this.ruleFinder.FindValidationRulesInTree(type, RuleType.Validation);
                this.ruleFinder.FindValidationRulesInTree(type, RuleType.Disabling);
            }

            if (serviceProvider == null)
            {
                return;
            }

            foreach (var (type, _) in sets)
            {
                this.translator.TranslateValidationRules(type, serviceProvider, new Dictionary<string, object>()).GetAwaiter().GetResult();
            }
        }
    }
}