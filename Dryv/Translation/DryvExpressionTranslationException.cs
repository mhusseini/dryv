using System;
using System.Linq.Expressions;

namespace Dryv.Translation
{
    public class DryvExpressionTranslationException : DryvException
    {
        public Expression Expression { get; }

        public DryvExpressionTranslationException(Expression expression, string message, Exception innerException)
            : base(GetMessage(expression, message), innerException)
        {
            Expression = expression;
        }

        public DryvExpressionTranslationException(Expression expression, string message)
            : base(GetMessage(expression, message))
        {
            Expression = expression;
        }

        private static string GetMessage(Expression expression, string message)
        {
            return message + " Expression: " + expression + ".";
        }
    }
}