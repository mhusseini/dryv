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
        private readonly RuleTranslationPhase ruleTranslationPhase;
        private readonly RuleResolutionPipeline pipeline;

        internal DryvTranslator(RuleTranslationPhase ruleTranslationPhase, RuleResolutionPipeline pipeline)
        {
            this.ruleTranslationPhase = ruleTranslationPhase;
            this.pipeline = pipeline;
        }

        public async Task<TranslationResult> TranslateValidationRules(Type modelType, IDryvServiceResolver serviceResolver, IReadOnlyDictionary<string, object> parameters = null)
        {
            var validationRules = ruleTranslationPhase
                .FindAndTranslateRulesInTree(modelType, RuleType.Validation)
                .Where(r => r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client))
                .ToList();

            var disablingRules = ruleTranslationPhase
                .FindAndTranslateRulesInTree(modelType, RuleType.Disabling)
                .Where(r => r.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client))
                .ToList();

            var ruleParameters = await pipeline.ResolveParametersAsync(validationRules.Union(disablingRules), serviceResolver, parameters);

            var enabledValidation = await pipeline.FilterEnabledRulesAsync(validationRules, serviceResolver, ruleParameters);
            var enabledDisabling = await pipeline.FilterEnabledRulesAsync(disablingRules, serviceResolver, ruleParameters);

            var clientValidation = enabledValidation.Select(rule => Translate(rule, serviceResolver, ruleParameters));
            var clientDisablers = enabledDisabling.Select(rule => Translate(rule, serviceResolver, ruleParameters));

            return new TranslationResult
            {
                ValidationFunctions = clientValidation.GroupBy(c => c.Rule.ModelPath, c => c).ToDictionary(i => i.Key, i => i.ToList()),
                DisablingFunctions = clientDisablers.GroupBy(c => c.Rule.ModelPath, c => c).ToDictionary(i => i.Key, i => i.ToList()),
                Parameters = ruleParameters.Values.Select(p => p.Values).Merge()
            };
        }

        private static TranslatedRule Translate(DryvCompiledRule rule, IDryvServiceResolver serviceResolver, IReadOnlyDictionary<IReadOnlyList<DryvCompiledRule>, DryvParameters> parameters)
        {
            var services = serviceResolver.GetServices(rule, parameters);
            var options = serviceResolver.GetService<DryvOptions>();

            var arguments = services;

            try
            {
                var translation = rule.Translation;
                var code = translation?.TranslatedValidationExpression(_ => null, arguments, options);

                return new TranslatedRule { Rule = rule, ClientCode = code, IsAsync = translation?.IsAsync ?? false };
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