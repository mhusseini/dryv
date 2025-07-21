using System.Linq;
using System.Linq.Expressions;
using Dryv.Rules;

namespace Dryv.Translation.Translators
{
    public class DryvParametersTranslator : MethodCallTranslator
    {
        public DryvParametersTranslator()
        {
            this.Supports<DryvParameters>();
            this.AddMethodTranslator(nameof(DryvParameters.Get), Get);
        }

        private void Get(MethodTranslationContext context)
        {
            context.Writer.Write("($ctx.parameter ? $ctx.parameter(");
            context.Translator.Translate(context.Expression.Arguments.First(), context);
            context.Writer.Write(") : ");
            context.InjectRuntimeExpression(context.Expression, context.Expression.Object as ParameterExpression);
            context.Writer.Write(")");
        }
    }
}