using System.Linq;
using System.Linq.Expressions;
using Dryv.Configuration;
using Dryv.Rules;

namespace Dryv.Translation.Translators
{
    public class DryvParametersTranslator : MethodCallTranslator
    {
        public DryvParametersTranslator(DryvOptions options)
        {
            if (options.DisableParameterInjection)
            {
                this.Supports<DryvParameters>();
                this.AddMethodTranslator(nameof(DryvParameters.Get), Get);
            }
        }

        private void Get(MethodTranslationContext context)
        {
            context.Writer.Write("$ctx.parameter(");
            context.Translator.Translate(context.Expression.Arguments.First(), context);
            context.Writer.Write(")");
        }
    }
}