using System.Linq;
using System.Linq.Expressions;
using Dryv.Extensions;

namespace Dryv.Translation.Translators
{
    public class ObjectTranslator : IDryvCustomTranslator
    {
        public int? OrderIndex { get; set; }
        public bool? AllowSurroundingBrackets(Expression expression) => false;

        public bool TryTranslate(CustomTranslationContext context)
        {
            if (!(context.Expression is MethodCallExpression methodCallExpression))
            {
                return false;
            }

            if (methodCallExpression.Method.Name != nameof(this.ToString) || methodCallExpression.Object == null)
            {
                return false;
            }

            context.Writer.Write("$ctx.format(");
            context.Translator.Translate(methodCallExpression.Object, context);
            context.Writer.Write($@", ""{methodCallExpression.Object.GetOriginalType().Name.ToLower()}""");
            
            if (methodCallExpression.Arguments.Any())
            {
                context.Writer.Write(", ");
                context.Translator.Translate(methodCallExpression.Arguments.First(), context);
            }

            context.Writer.Write(")");
            
            return true;
        }
    }
}