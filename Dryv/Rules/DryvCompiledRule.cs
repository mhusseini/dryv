using System;
using System.Linq.Expressions;

namespace Dryv.Rules
{
    public class DryvCompiledRule : DryvRuleDefinition
    {
        internal DryvCompiledRule()
        {
        }

        internal DryvCompiledRule(DryvRuleDefinition source) : base(source)
        {
        }

        internal DryvCompiledRule(DryvCompiledRule source) : base(source)
        {
            this.CompiledEnablingExpression = source.CompiledEnablingExpression;
            this.CompiledValidationExpression = source.CompiledValidationExpression;
        }

        public Func<object[], object> CompiledEnablingExpression { get; internal set; }
        public Func<object, object[], object> CompiledValidationExpression { get; internal set; }

        internal static DryvCompiledRule CreateParameter(string name, Expression<Func<object, object[], object>> lambda, Type[] services)
        {
            return new DryvCompiledRule
            {
                RuleType = RuleType.Parameter,
                Name = name,
                ServiceTypes = services,
                CompiledValidationExpression = lambda.Compile()
            };
        }
    }
}