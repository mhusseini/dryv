using System.Collections.Generic;
using Dryv.Rules;

namespace Dryv
{
    /// <summary>
    /// Base class for a collection of Dryv validation rules.
    /// Use <see cref="For{TModel}"/> to begin building rules for a specific model type.
    /// </summary>
    public abstract class DryvRules
    {
        /// <summary>Gets the disabling rules registered on this instance.</summary>
        public IReadOnlyList<DryvRuleDefinition> DisablingRules => this.InternalDisablingRules;
        /// <summary>Gets the named parameters registered on this instance.</summary>
        public IReadOnlyList<DryvCompiledRule> Parameters => this.InternalParameters;
        /// <summary>Gets the validation rules registered on this instance.</summary>
        public IReadOnlyList<DryvRuleDefinition> ValidationRules => this.InternalValidationRules;
        internal List<DryvRuleDefinition> InternalDisablingRules { get; } = new();
        internal List<DryvCompiledRule> InternalParameters { get; } = new();
        internal List<DryvRuleDefinition> InternalValidationRules { get; } = new();

        /// <summary>
        /// Begins building a new set of validation rules for <typeparamref name="TModel"/>.
        /// Chain <c>.Rule()</c>, <c>.ServerRule()</c>, <c>.DisableRules()</c>, and
        /// <c>.Parameter()</c> calls on the returned instance to register rules.
        /// </summary>
        /// <typeparam name="TModel">The model type the rules apply to.</typeparam>
        /// <returns>A new <see cref="DryvRules{TModel}"/> instance.</returns>
        public static DryvRules<TModel> For<TModel>() => new();
    }
}