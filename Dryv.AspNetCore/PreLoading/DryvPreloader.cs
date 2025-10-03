using Dryv.RuleDetection;
using Dryv.Rules;
using Microsoft.Extensions.Options;

namespace Dryv.AspNetCore.PreLoading
{
    internal class DryvPreloader
    {
        private readonly IOptions<DryvPreloadingOptions> options;
        private readonly DryvRuleFinder ruleFinder;
        private bool hasStarted;

        public DryvPreloader(DryvRuleFinder ruleFinder, IOptions<DryvPreloadingOptions> options)
        {
            this.ruleFinder = ruleFinder;
            this.options = options;
        }

        public void Preload()
        {
            if (this.hasStarted || !this.options.Value.IsEnabled)
            {
                return;
            }

            this.hasStarted = true;

            foreach (var (type, _) in DryvSets.GetDryvSets())
            {
                this.ruleFinder.FindValidationRulesInTree(type, RuleType.Validation);
                this.ruleFinder.FindValidationRulesInTree(type, RuleType.Disabling);
            }
        }
    }
}