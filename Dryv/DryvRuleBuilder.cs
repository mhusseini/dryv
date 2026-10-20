using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dryv.Extensions;
using Dryv.Rules;

namespace Dryv
{
    public partial class DryvRuleBuilder<TModel>
    {
        private readonly DryvRules<TModel> _rules;
        private readonly Expression<Func<TModel, object>>[] _properties;
        private int _ruleStartIndex = -1;

        internal DryvRuleBuilder(DryvRules<TModel> rules, params Expression<Func<TModel, object>>[] properties)
        {
            _rules = rules;
            _properties = properties;
        }

        internal void AddRule(LambdaExpression rule, params Type[] services)
        {
            _ruleStartIndex = _rules.InternalValidationRules.Count;
            _rules.Add(rule, _properties, null, null, services);
        }

        internal void SetDisableCondition(LambdaExpression condition)
        {
            for (var i = _ruleStartIndex; i < _rules.InternalValidationRules.Count; i++)
            {
                _rules.InternalValidationRules[i].EnablingExpression = condition;
            }
        }

        public DryvRuleBuilder<TModel> EvaluateOn(DryvEvaluationLocation location)
        {
            for (var i = _ruleStartIndex; i < _rules.InternalValidationRules.Count; i++)
            {
                _rules.InternalValidationRules[i].EvaluationLocation = location;
            }
            return this;
        }

        public DryvRuleBuilder<TModel> RelatedTo(params Expression<Func<TModel, object>>[] relatedProperties)
        {
            var related = relatedProperties
                .Select(p => p.GetMemberExpression())
                .Where(e => e != null)
                .ToDictionary(
                    e => (PropertyInfo)e.Member,
                    e => e.GetModelPath(false, out _));

            for (var i = _ruleStartIndex; i < _rules.InternalValidationRules.Count; i++)
            {
                _rules.InternalValidationRules[i].RelatedProperties = related;
            }
            return this;
        }

        public DryvRuleBuilder<TModel> Group(string group)
        {
            for (var i = _ruleStartIndex; i < _rules.InternalValidationRules.Count; i++)
            {
                _rules.InternalValidationRules[i].Group = group;
            }
            return this;
        }

        public DryvRuleBuilder<TModel> Name(string name)
        {
            for (var i = _ruleStartIndex; i < _rules.InternalValidationRules.Count; i++)
            {
                _rules.InternalValidationRules[i].Name = name;
            }
            return this;
        }

        public DryvRuleBuilder<TModel> Annotate(string key, object value)
        {
            for (var i = _ruleStartIndex; i < _rules.InternalValidationRules.Count; i++)
            {
                _rules.InternalValidationRules[i].Annotations[key] = value;
            }
            return this;
        }
    }
}
