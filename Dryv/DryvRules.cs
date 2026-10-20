using System.Collections.Generic;
using Dryv.Rules;

namespace Dryv
{
    public abstract class DryvRules
    {
        public IReadOnlyList<DryvRuleDefinition> DisablingRules => this.InternalDisablingRules;
        public IReadOnlyList<DryvCompiledRule> Parameters => this.InternalParameters;
        public IReadOnlyList<DryvRuleDefinition> ValidationRules => this.InternalValidationRules;
        internal List<DryvRuleDefinition> InternalDisablingRules { get; } = new();
        internal List<DryvCompiledRule> InternalParameters { get; } = new();
        internal List<DryvRuleDefinition> InternalValidationRules { get; } = new();

        public static DryvRules<TModel> For<TModel>() => new();
    }
}