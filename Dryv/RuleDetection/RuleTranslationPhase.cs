using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dryv.Configuration;
using Dryv.Rules;
using Dryv.Translation;

namespace Dryv.RuleDetection
{
    public class RuleTranslationPhase(DryvRuleFinder ruleFinder, ITranslator translator, DryvOptions options)
    {
        private readonly ConcurrentDictionary<string, IEnumerable<DryvCompiledRule>> translatedRuleTreeCache = new();

        public void ClearCache()
        {
            translatedRuleTreeCache.Clear();
        }

        public IEnumerable<DryvCompiledRule> FindAndTranslateRulesInTree(Type rootType, RuleType ruleType)
        {
            return translatedRuleTreeCache.GetOrAdd($"{rootType.FullName}|{ruleType}", _ =>
            {
                return ruleFinder.FindValidationRulesInTree(rootType, ruleType)
                    .Select(rule => rule.EvaluationLocation.HasFlag(DryvEvaluationLocation.Client)
                        ? Translate(rule, rule.ValidationExpression)
                        : rule)
                    .ToList();
            });
        }

        private DryvCompiledRule Translate(DryvCompiledRule compiledRule, Expression validationExpression)
        {
            if (translator == null)
            {
                return compiledRule;
            }

            try
            {
                var translation = translator.Translate(validationExpression, compiledRule.PropertyExpression, compiledRule);
                return compiledRule.WithTranslation(
                    new TranslationOutput
                    {
                        TranslatedValidationExpression = translation.Factory,
                        CodeTemplate = translation.CodeTemplate,
                        IsAsync = translation.IsAsync,
                    },
                    translation.InjectedServiceTypes);
            }
            catch (DryvException ex)
            {
                if (options.TranslationErrorBehavior != TranslationErrorBehavior.ValidateOnServer)
                {
                    throw new DryvExpressionTranslationException(validationExpression, "An error occurred while translating validation rule.", ex);
                }

                return compiledRule.WithTranslation(new TranslationOutput { TranslationError = ex });
            }
            catch (Exception ex)
            {
                throw new DryvExpressionTranslationException(validationExpression, "An error occurred while translating validation rule.", ex);
            }
        }
    }
}
