using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dryv.Extensions;

namespace Dryv
{
    /// <summary>
    /// Fluent builder for configuring validation rules within a <see cref="DryvRules{TModel}.Rule"/> or
    /// <see cref="DryvRules{TModel}.ServerRule"/> callback. Use <c>Validate</c> / <c>ServerValidate</c>
    /// to register rule expressions, then chain configuration methods such as <see cref="Disable"/>,
    /// <see cref="EvaluateOn"/>, <see cref="Group"/>, <see cref="Name"/>, <see cref="RelatedTo"/>,
    /// and <see cref="Annotate"/> to customise rule behaviour.
    /// </summary>
    /// <typeparam name="TModel">The model type the rules apply to.</typeparam>
    public partial class DryvRuleBuilder<TModel>
    {
        private readonly DryvRules<TModel> rules;
        private readonly Expression<Func<TModel, object>>[] properties;
        private int ruleStartIndex = -1;

        internal DryvRuleBuilder(DryvRules<TModel> rules, params Expression<Func<TModel, object>>[] properties)
        {
            this.rules = rules;
            this.properties = properties;
        }

        private void AddRule(LambdaExpression rule, params Type[] services)
        {
            ruleStartIndex = rules.InternalValidationRules.Count;
            rules.Add(rule, properties, null, null, services);
        }

        private void AddServerRule(Delegate rule, params Type[] services)
        {
            ruleStartIndex = rules.InternalValidationRules.Count;
            rules.AddServer(rule, properties, null, null, services);
        }

        private void SetDisableCondition(LambdaExpression condition)
        {
            for (var i = ruleStartIndex; i < rules.InternalValidationRules.Count; i++)
            {
                rules.InternalValidationRules[i].EnablingExpression = condition;
            }
        }

        /// <summary>
        /// Restricts where the preceding validation rule is evaluated.
        /// By default, rules run on both server and client.
        /// </summary>
        /// <param name="location">
        /// A <see cref="DryvEvaluationLocation"/> value indicating whether the rule should run
        /// on the server, the client, or both.
        /// </param>
        /// <returns>This builder instance for further chaining.</returns>
        public DryvRuleBuilder<TModel> EvaluateOn(DryvEvaluationLocation location)
        {
            for (var i = ruleStartIndex; i < rules.InternalValidationRules.Count; i++)
            {
                rules.InternalValidationRules[i].EvaluationLocation = location;
            }
            return this;
        }

        /// <summary>
        /// Explicitly declares the properties whose changes should trigger re-validation
        /// of the preceding rule on the client.
        /// </summary>
        /// <param name="relatedProperties">
        /// One or more property selectors that identify the related properties.
        /// </param>
        /// <returns>This builder instance for further chaining.</returns>
        public DryvRuleBuilder<TModel> RelatedTo(params Expression<Func<TModel, object>>[] relatedProperties)
        {
            var related = relatedProperties
                .Select(p => p.GetMemberExpression())
                .Where(e => e != null)
                .ToDictionary(
                    e => (PropertyInfo)e.Member,
                    e => e.GetModelPath(false, out _));

            for (var i = ruleStartIndex; i < rules.InternalValidationRules.Count; i++)
            {
                rules.InternalValidationRules[i].RelatedProperties = related;
            }
            return this;
        }

        /// <summary>
        /// Assigns an explicit group name to the preceding validation rule.
        /// Grouped rules share a single error message slot on the client,
        /// which is useful for multi-property rules.
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <returns>This builder instance for further chaining.</returns>
        public DryvRuleBuilder<TModel> Group(string group)
        {
            for (var i = ruleStartIndex; i < rules.InternalValidationRules.Count; i++)
            {
                rules.InternalValidationRules[i].Group = group;
            }
            return this;
        }

        /// <summary>
        /// Assigns a name to the preceding validation rule.
        /// The name is included in the generated JavaScript output and can be used
        /// for identification on the client.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <returns>This builder instance for further chaining.</returns>
        public DryvRuleBuilder<TModel> Name(string name)
        {
            for (var i = ruleStartIndex; i < rules.InternalValidationRules.Count; i++)
            {
                rules.InternalValidationRules[i].Name = name;
            }
            return this;
        }

        /// <summary>
        /// Attaches a custom metadata annotation to the preceding validation rule.
        /// Annotations are included in the generated JavaScript and can be read by
        /// client-side code for custom UI behaviour.
        /// </summary>
        /// <param name="key">The annotation key.</param>
        /// <param name="value">The annotation value.</param>
        /// <returns>This builder instance for further chaining.</returns>
        public DryvRuleBuilder<TModel> Annotate(string key, object value)
        {
            for (var i = ruleStartIndex; i < rules.InternalValidationRules.Count; i++)
            {
                rules.InternalValidationRules[i].Annotations[key] = value;
            }
            return this;
        }
    }
}
