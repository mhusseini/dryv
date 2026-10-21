using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Dryv.Translation
{
    internal class AsyncPromiseWriter
    {
        private readonly JavaScriptTranslator translator;

        public AsyncPromiseWriter(JavaScriptTranslator translator)
        {
            this.translator = translator;
        }

        public void WriteAsyncPromiseChain(
            Dictionary<StringBuilder, ParameterExpression> asyncCalls,
            TranslationContext context,
            Action translateBody)
        {
            if (asyncCalls.Any())
            {
                context.IsAsync = true;
            }

            foreach (var call in asyncCalls)
            {
                context.Writer.Write(call.Key.ToString());
                context.Writer.Write(".then(function(");
                context.Writer.Write(call.Value.Name);
                context.Writer.Write("){return ");
            }

            translateBody();

            for (var i = 0; i < asyncCalls.Count; i++)
            {
                context.Writer.Write(";})");
            }
        }

        public Expression TranslateAsyncBooleanChain(BinaryExpression chain, TranslationContext context, List<Expression> asyncExpressions)
        {
            if (asyncExpressions.Contains(chain.Left))
            {
                return this.TranslateAsyncBooleanOperand(chain.Left, context, asyncExpressions);
            }

            this.translator.Translate(chain.Left, context);

            this.translator.TryWriteTerminal(chain, context.Writer);

            if (asyncExpressions.Contains(chain.Right))
            {
                return this.TranslateAsyncBooleanOperand(chain.Right, context, asyncExpressions);
            }

            this.translator.Translate(chain.Right, context);

            return Expression.Empty();
        }

        private Expression TranslateAsyncBooleanOperand(Expression expression, TranslationContext context, List<Expression> asyncExpressions)
        {
            if (expression is BinaryExpression chain && (chain.NodeType == ExpressionType.OrElse || chain.NodeType == ExpressionType.AndAlso))
            {
                return this.TranslateAsyncBooleanChain(chain, context, asyncExpressions);
            }

            return expression;
        }
    }
}
