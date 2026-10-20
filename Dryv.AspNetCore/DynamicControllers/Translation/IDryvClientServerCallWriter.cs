using System.Collections.Generic;
using System.Linq.Expressions;
using Dryv.Translation;

namespace Dryv.AspNetCore.DynamicControllers.Translation
{
    /// <summary>
    /// Emits client-side code that calls a dynamically generated server endpoint
    /// for rule expressions that cannot be translated to the client language.
    /// </summary>
    public interface IDryvClientServerCallWriter
    {
        /// <summary>
        /// Writes client code that sends the specified model member values to the server endpoint.
        /// </summary>
        /// <param name="context">The current translation context.</param>
        /// <param name="translator">The translator used to format identifiers and values.</param>
        /// <param name="url">The URL of the dynamically generated server endpoint.</param>
        /// <param name="httpMethod">The HTTP method (e.g. POST, GET) for the request.</param>
        /// <param name="modelMembers">The model member expressions whose values should be sent.</param>
        void Write(TranslationContext context, IExpressionWriter translator, string url, string httpMethod, IList<MemberExpression> modelMembers);

        /// <summary>
        /// Writes client code that sends the entire model object to the server endpoint.
        /// </summary>
        /// <param name="context">The current translation context.</param>
        /// <param name="translator">The translator used to format identifiers and values.</param>
        /// <param name="url">The URL of the dynamically generated server endpoint.</param>
        /// <param name="httpMethod">The HTTP method (e.g. POST, GET) for the request.</param>
        /// <param name="modelParameter">The parameter expression representing the model.</param>
        void Write(TranslationContext context, IExpressionWriter translator, string url, string httpMethod, ParameterExpression modelParameter);

        /// <summary>
        /// Writes client code that calls the server endpoint without sending any model data.
        /// </summary>
        /// <param name="context">The current translation context.</param>
        /// <param name="translator">The translator used to format identifiers and values.</param>
        /// <param name="url">The URL of the dynamically generated server endpoint.</param>
        /// <param name="httpMethod">The HTTP method (e.g. POST, GET) for the request.</param>
        void Write(TranslationContext context, IExpressionWriter translator, string url, string httpMethod);
    }
}