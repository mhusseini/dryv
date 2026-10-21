using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dryv.AspNetCore
{
    /// <summary>
    /// Provides named parameter values for Dryv rule evaluation during an MVC action execution.
    /// Implementations can extract parameter values from the current HTTP request or action context.
    /// </summary>
    public interface IDryParameterProvider
    {
        /// <summary>
        /// Returns a dictionary of named parameter values to pass to Dryv rules
        /// for the current request.
        /// </summary>
        /// <param name="context">The action executing context for the current MVC request.</param>
        /// <returns>A read-only dictionary of parameter names to values.</returns>
        IReadOnlyDictionary<string, object> GetParameters(ActionExecutingContext context);
    }
}