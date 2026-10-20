using System.Collections.Generic;
using Dryv.Rules;

namespace Dryv
{
    public abstract class DryvRules
    {
        public IReadOnlyList<DryvRuleDefinition> DisablingRules => this.InternalDisablingRules;
        public IReadOnlyList<DryvCompiledRule> Parameters => this.InternalParameters;
        public IReadOnlyList<DryvRuleDefinition> ValidationRules => this.InternalValidationRules;
        internal List<DryvRuleDefinition> InternalDisablingRules { get; } = new List<DryvRuleDefinition>();
        internal List<DryvCompiledRule> InternalParameters { get; } = new List<DryvCompiledRule>();
        internal List<DryvRuleDefinition> InternalValidationRules { get; } = new List<DryvRuleDefinition>();

        public static DryvRules<TModel> For<TModel>() => new DryvRules<TModel>();
    }
}