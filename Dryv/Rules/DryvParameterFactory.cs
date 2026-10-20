using System;
using System.Linq.Expressions;

namespace Dryv.Rules
{
    /// <summary>
    /// Factory for creating <see cref="DryvCompiledRule"/> instances that represent
    /// named parameters (as opposed to validation or disabling rules).
    /// Extracted from <see cref="DryvCompiledRule"/> to keep rule types focused on
    /// holding state rather than constructing specialised variants.
    /// </summary>
    internal static class DryvParameterFactory
    {
        /// <summary>
        /// Creates a compiled rule that acts as a named parameter provider.
        /// </summary>
        /// <param name="name">The parameter name used for lookup at evaluation time.</param>
        /// <param name="lambda">An expression that, when compiled, produces the parameter value
        /// given a (unused) model and an array of resolved services.</param>
        /// <param name="services">The service types required to evaluate the parameter.</param>
        /// <returns>A <see cref="DryvCompiledRule"/> with <see cref="RuleType.Parameter"/>.</returns>
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
