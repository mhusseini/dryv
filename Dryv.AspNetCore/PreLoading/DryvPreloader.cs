using System;
using System.Collections.Generic;
using System.Linq;
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

            var types = GetAllValidatableTypes();

            foreach (var type in types)
            {
                this.ruleFinder.FindValidationRulesInTree(type, RuleType.Validation);
                this.ruleFinder.FindValidationRulesInTree(type, RuleType.Disabling);
            }

            if (serviceProvider == null)
            {
                return;
            }

            foreach (var type in types)
            {
                this.translator.TranslateValidationRules(type, serviceProvider, new Dictionary<string, object>()).GetAwaiter().GetResult();
            }
        }

        private static List<Type> GetAllValidatableTypes()
        {
            var dryvSetTypes = DryvSets.GetDryvSets().Select(s => s.Type);

            var dryvValidationTypes = from a in AppDomain.CurrentDomain.GetAssemblies()
                from t in a.GetTypes()
                where t.IsClass && t.GetCustomAttributes(typeof(DryvValidationAttribute), true).Length > 0
                select t;

            return dryvSetTypes
                .Union(dryvValidationTypes)
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToList();
        }
    }
}