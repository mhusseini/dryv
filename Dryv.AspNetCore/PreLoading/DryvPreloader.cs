using System;
using System.Collections.Generic;
using System.Linq;
using Dryv.RuleDetection;
using Dryv.Rules;
using Microsoft.Extensions.Options;

namespace Dryv.AspNetCore.PreLoading
{
    internal class DryvPreloader(RuleTranslationPhase ruleTranslationPhase, DryvTranslator translator, IOptions<DryvPreloadingOptions> options)
    {
        private bool hasStarted;

        public void Preload(IDryvServiceResolver serviceResolver = null)
        {
            if (this.hasStarted || !options.Value.IsEnabled)
            {
                return;
            }

            this.hasStarted = true;

            var types = GetAllValidatableTypes();

            foreach (var type in types)
            {
                ruleTranslationPhase.FindAndTranslateRulesInTree(type, RuleType.Validation);
                ruleTranslationPhase.FindAndTranslateRulesInTree(type, RuleType.Disabling);
            }

            if (serviceResolver == null)
            {
                return;
            }

            foreach (var type in types)
            {
                translator.TranslateValidationRules(type, serviceResolver, new Dictionary<string, object>()).GetAwaiter().GetResult();
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