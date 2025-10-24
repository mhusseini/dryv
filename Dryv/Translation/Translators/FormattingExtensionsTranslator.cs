using Dryv.Extensions;

namespace Dryv.Translation.Translators
{
    public class FormattingExtensionsTranslator : MethodCallTranslator
    {
        public FormattingExtensionsTranslator()
        {
            this.Supports(typeof(FormattingExtensions));

            this.AddMethodTranslator(nameof(FormattingExtensions.ToFormattedString), ToFormattedString);
        }

        private static void ToFormattedString(MethodTranslationContext context)
        {
            context.Writer.Write("$ctx.format(");
            context.Translator.Translate(context.Expression.Arguments[0], context);
            context.Writer.Write($@", ""{context.Expression.GetOriginalType().Name.ToLower()}""");
            context.Writer.Write(")");
        }
    }
}