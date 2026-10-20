using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dryv.Extensions;
using Dryv.Rules;

namespace Dryv
{
    public class RuleResolutionPipeline
    {
        public Task<Dictionary<IReadOnlyList<DryvCompiledRule>, DryvParameters>> ResolveParametersAsync(
            IEnumerable<DryvCompiledRule> rules,
            IDryvServiceResolver serviceResolver,
            IReadOnlyDictionary<string, object> parameters = null)
        {
            return DryvParametersHelper.GetDryvParameters(rules, serviceResolver, parameters);
        }

        public async Task<bool> IsRuleEnabledAsync(
            DryvCompiledRule rule,
            IDryvServiceResolver serviceResolver,
            IReadOnlyDictionary<IReadOnlyList<DryvCompiledRule>, DryvParameters> ruleParameters)
        {
            var services = serviceResolver.GetServices(rule, ruleParameters);
            return true.Equals(await TaskValueHelper.GetPossiblyAsyncValue(rule.CompiledEnablingExpression(services)));
        }

        public async Task<List<DryvCompiledRule>> FilterEnabledRulesAsync(
            IEnumerable<DryvCompiledRule> rules,
            IDryvServiceResolver serviceResolver,
            IReadOnlyDictionary<IReadOnlyList<DryvCompiledRule>, DryvParameters> ruleParameters)
        {
            var tasks = rules.Select(rule =>
                IsRuleEnabledAsync(rule, serviceResolver, ruleParameters)
                    .ContinueWith(task => task.Result ? rule : null));

            return (from rule in await Task.WhenAll(tasks)
                    where rule != null
                    select rule).ToList();
        }
    }
}
