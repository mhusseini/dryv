using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dryv.Configuration;
using Dryv.Extensions;
using Dryv.RuleDetection;
using Dryv.Rules;
using Dryv.Translation;

namespace Dryv
{
    public class DryvTranslator
    {
        private readonly DryvRuleFinder ruleFinder;

        public DryvTranslator(DryvRuleFinder ruleFinder)
        {
            this.ruleFinder = ruleFinder;
        }

        public async Task<TranslationResult> TranslateValidationRules(Type modelType, IDryvServiceResolver serviceResolver, IReadOnlyDictionary<string, object> parameters = null)
        {
            var validationRules = this.ruleFinder
                .FindValidationRulesInTree(modelType, RuleType.Validation)
                .Where(r => r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client))
                .ToList();

            var disablingRules = this.ruleFinder
                .FindValidationRulesInTree(modelType, RuleType.Disabling)
                .Where(r => r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client))
                .ToList();

            var ruleParameters = await DryvParametersHelper.GetDryvParameters(validationRules.Union(disablingRules), serviceResolver, parameters);

            var validationTasks = validationRules.Select(rule =>
                IsRuleEnabled(rule, serviceResolver, ruleParameters)
                    .ContinueWith(task => task.Result ? rule : null));

            var clientValidation = from rule in await Task.WhenAll(validationTasks)
                where rule != null
                select Translate(rule, serviceResolver, ruleParameters);

            var disablingTasks = disablingRules.Select(rule =>
                IsRuleEnabled(rule, serviceResolver, ruleParameters)
                    .ContinueWith(task => task.Result ? rule : null));

            var clientDisablers = from rule in await Task.WhenAll(disablingTasks)
                where rule != null
                select Translate(rule, serviceResolver, ruleParameters);

            return new TranslationResult
            {
                ValidationFunctions = clientValidation.GroupBy(c => c.Rule.ModelPath, c => c).ToDictionary(i => i.Key, i => i.ToList()),
                DisablingFunctions = clientDisablers.GroupBy(c => c.Rule.ModelPath, c => c).ToDictionary(i => i.Key, i => i.ToList()),
                Parameters = ruleParameters.Values.Select(p => p.Values).Merge()
            };
        }

        private static async Task<bool> IsRuleEnabled(DryvCompiledRule rule, IDryvServiceResolver serviceResolver, IReadOnlyDictionary<IReadOnlyList<DryvCompiledRule>, DryvParameters> parameters)
        {
            var arguments = serviceResolver.GetServices(rule, parameters);
            return true.Equals(await TaskValueHelper.GetPossiblyAsyncValue(rule.CompiledEnablingExpression(arguments)));
        }

        private static TranslatedRule Translate(DryvCompiledRule rule, IDryvServiceResolver serviceResolver, IReadOnlyDictionary<IReadOnlyList<DryvCompiledRule>, DryvParameters> parameters)
        {
            var services = serviceResolver.GetServices(rule, parameters);
            var options = serviceResolver.GetService<DryvOptions>();

            var arguments = services;

            try
            {
                var translatedRule = rule as DryvTranslatedRule;
                var code = translatedRule?.TranslatedValidationExpression(_ => null, arguments, options);

                return new TranslatedRule { Rule = rule, ClientCode = code, IsAsync = translatedRule?.IsAsync ?? false };
            }
            catch (Exception ex)
            {
                throw CreateException("An error occurred while translating validation rule.", ex, rule);
            }
        }

        private static DryvTranslationException CreateException(string msg, Exception ex, DryvCompiledRule rule)
        {
            var sb = new StringBuilder(msg);

            sb.AppendLine();
            sb.Append("The error occurred while translating the following rule for property ");
            sb.Append(rule.Property.Name);
            sb.Append(" on type ");
            sb.Append(rule.ModelType.FullName);
            sb.AppendLine(":");
            sb.Append(rule.ValidationExpression);

            return new DryvTranslationException(sb.ToString(), ex);
        }
    }
}