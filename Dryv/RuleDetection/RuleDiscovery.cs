using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dryv.Rules;
using BindingFlags = System.Reflection.BindingFlags;

namespace Dryv.RuleDetection
{
    internal class RuleDiscovery
    {
        private const BindingFlags BindingFlagsForRules = BindingFlags.Static |
                                                          BindingFlags.Public |
                                                          BindingFlags.NonPublic |
                                                          BindingFlags.FlattenHierarchy;

        private readonly ConcurrentDictionary<string, IEnumerable<DryvRuleDefinition>> cache = new();

        public void ClearCache()
        {
            cache.Clear();
        }

        public IEnumerable<DryvRuleDefinition> FindRulesOnType(Type type, RuleType ruleType)
        {
            return cache.GetOrAdd($"{type.FullName}|{ruleType}", _ =>
            {
                var typeInfo = type.GetTypeInfo();

                var fromFields = from p in typeInfo.GetFields(BindingFlagsForRules)
                                 where typeof(DryvRules).IsAssignableFrom(p.FieldType)
                                 select p.GetValue(null) as DryvRules;

                var fromProperties = from p in typeInfo.GetProperties(BindingFlagsForRules)
                                     where typeof(DryvRules).IsAssignableFrom(p.PropertyType)
                                     select p.GetValue(null) as DryvRules;

                var fromMethods = from m in typeInfo.GetMethods(BindingFlagsForRules)
                                  where m.IsStatic
                                        && !m.GetParameters().Any()
                                        && typeof(DryvRules).IsAssignableFrom(m.ReturnType)
                                        && !m.ContainsGenericParameters
                                  select m.Invoke(null, null) as DryvRules;

                return (from rules in fromFields.Union(fromProperties).Union(fromMethods)
                        from rule in GetRulesOfType(rules, ruleType)
                        select rule).ToList();
            });
        }

        private static IEnumerable<DryvRuleDefinition> GetRulesOfType(DryvRules rules, RuleType ruleType)
        {
            return ruleType == RuleType.Disabling ? rules.DisablingRules : rules.ValidationRules;
        }
    }
}
