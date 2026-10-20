using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dryv.Configuration;
using Dryv.Extensions;
using Dryv.RuleDetection;
using Dryv.Rules;
using Dryv.Validation;

namespace Dryv
{
    public class DryvValidator(DryvRuleFinder ruleFinder, DryvOptions options)
    {
        private readonly ConcurrentDictionary<Type, GroupedValidation> cache = new();

        public void ClearCache()
        {
            this.cache.Clear();
            ruleFinder.ClearCache();
        }

        public async Task<IDictionary<string, DryvValidationResult>> Validate(object model, IDryvServiceResolver serviceResolver, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (model == null)
            {
                return new Dictionary<string, DryvValidationResult>();
            }

            var modelType = model.GetType();
            var validation = cache.GetOrAdd(modelType, this.GroupValidation);

            var rules = validation.ValidationRules.SelectMany(i => i.Value).Union(validation.DisablingRules.SelectMany(i => i.Value));
            var ruleParameters = await DryvParametersHelper.GetDryvParameters(rules, serviceResolver, parameters);

            var validationTasks = new List<Task<KeyValuePair<string, DryvValidationResult>?>>();
            foreach (var kvp in validation.ValidationRules)
            {
                if (await this.IsSubtreeDisabledAsync(model, kvp.Key, validation.DisablingRules, serviceResolver, ruleParameters))
                {
                    continue;
                }

                validationTasks.Add(this.GetFirstValidationError(model, serviceResolver, kvp, ruleParameters));
            }

            var taskResults = await Task.WhenAll(validationTasks);

            return (from result in taskResults
                    where result.HasValue
                    select result.Value)
                .ToDictionary(i => i.Key, i => i.Value);
        }

        private static async Task<DryvValidationResult> TryGetValidationResult(object model, DryvCompiledRule rule, object[] services)
        {
            switch (rule.CompiledValidationExpression(model, services))
            {
                case DryvValidationResult r when r.IsSuccess():
                    return null;

                case DryvValidationResult r:
                    return r;

                case Task<DryvValidationResult> task:
                    var result = await task;
                    return result.IsSuccess() ? null : result;

                default:
                    return null;
            }
        }

        private async Task<KeyValuePair<string, DryvValidationResult>?> GetFirstValidationError(object model, IDryvServiceResolver serviceResolver, KeyValuePair<string, List<DryvCompiledRule>> kvp, Dictionary<IReadOnlyList<DryvCompiledRule>, DryvParameters> parameters)
        {
            foreach (var rule in kvp.Value)
            {
                try
                {
                    var services = serviceResolver.GetServices(rule, parameters);
                    if (!true.Equals(await TaskValueHelper.GetPossiblyAsyncValue(rule.CompiledEnablingExpression(services))))
                    {
                        continue;
                    }

                    var result = await TryGetValidationResult(model, rule, services);
                    if (result == null)
                    {
                        continue;
                    }

                    result.Group = rule.Group;

                    return new KeyValuePair<string, DryvValidationResult>(kvp.Key, result);
                }
                catch (Exception ex)
                {
                    throw this.ThrowValidationException(model, rule, ex, RuleType.Validation);
                }
            }

            return null;
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

        private async Task<bool> IsSubtreeDisabledAsync(object model, string modelPath, IReadOnlyDictionary<string, List<DryvCompiledRule>> disablingRules, IDryvServiceResolver serviceResolver, Dictionary<IReadOnlyList<DryvCompiledRule>, DryvParameters> parameters)
        {
            var rules = FindDisablingRules(modelPath, disablingRules);
            if (rules == null)
            {
                return false;
            }

            foreach (var rule in rules)
            {
                try
                {
                    var services = serviceResolver.GetServices(rule, parameters);
                    var o = await TaskValueHelper.GetPossiblyAsyncValue(rule.CompiledValidationExpression(model, services));

                    if (!(o is bool) || (bool)o)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    throw this.ThrowValidationException(model, rule, ex, RuleType.Disabling);
                }
            }

            return false;
        }

        private static List<DryvCompiledRule> FindDisablingRules(string modelPath, IReadOnlyDictionary<string, List<DryvCompiledRule>> disablingRules)
        {
            var searchIndex = -1;

            while (true)
            {
                searchIndex = modelPath.IndexOf('.', searchIndex + 1);
                var key = searchIndex < 0 ? modelPath : modelPath.Substring(0, searchIndex);

                if (disablingRules.TryGetValue(key, out var result))
                {
                    return result;
                }

                if (searchIndex < 0)
                {
                    break;
                }
            }

            return null;
        }

        private Exception ThrowValidationException(object model, DryvCompiledRule rule, Exception innerException, RuleType ruleType)
        {
            var expressionText = rule?.ValidationExpression?.ToString() ?? "unknown";
            var propertyName = rule?.Property != null 
                ? $"{rule.Property.DeclaringType?.Name ?? "Unknown"}.{rule.Property.Name}" 
                : "unknown property";
            var sb = new StringBuilder($"An error occurred executing the {ruleType.ToString().ToLowerInvariant()} expression '{expressionText}' for property '{propertyName}'. See the inner exception for details.");
            if (options?.IncludeModelDataInExceptions == true)
            {
                var json = options.JsonConversion?.Invoke(model) ?? model?.ToString();
                sb.AppendLine("The model being validated is:");
                sb.Append(json);
            }

            return new DryvValidationException(sb.ToString(), innerException);
        }

        private class GroupedValidation
        {
            public Dictionary<string, List<DryvCompiledRule>> DisablingRules { get; set; }

            public Dictionary<string, List<DryvCompiledRule>> ValidationRules { get; set; }
        }
    }
}