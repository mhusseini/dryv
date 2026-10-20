using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Dryv.RuleDetection;
using Dryv.Rules;

namespace Dryv
{
    public class DryvValidationCache(DryvRuleFinder ruleFinder)
    {
        private readonly ConcurrentDictionary<Type, GroupedValidation> cache = new();

        public GroupedValidation GetValidation(Type modelType)
            => cache.GetOrAdd(modelType, GroupValidation);

        public void Clear()
        {
            cache.Clear();
            ruleFinder.ClearCache();
        }

        private GroupedValidation GroupValidation(Type type)
        {
            return new GroupedValidation
            {
                ValidationRules = ruleFinder.FindValidationRulesInTree(type, RuleType.Validation)
                    .Where(r => r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Server))
                    .GroupBy(r => r.ModelPath)
                    .ToDictionary(g => g.Key, g => g.ToList()),

                DisablingRules = ruleFinder.FindValidationRulesInTree(type, RuleType.Disabling)
                    .Where(r => r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Server))
                    .GroupBy(r => r.ModelPath)
                    .ToDictionary(g => g.Key, g => g.ToList())
            };
        }

        public class GroupedValidation
        {
            public Dictionary<string, List<DryvCompiledRule>> DisablingRules { get; set; }

            public Dictionary<string, List<DryvCompiledRule>> ValidationRules { get; set; }
        }
    }
}
