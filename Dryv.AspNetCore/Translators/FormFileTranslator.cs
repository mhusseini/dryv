using Dryv.Translation;
using Dryv.Translation.Translators;
using Microsoft.AspNetCore.Http;

namespace Dryv.AspNetCore.Translators
{
    public class FormFileTranslator : MethodCallTranslator
    {
        public FormFileTranslator()
        {
            this.Supports<IFormFile>();

            this.AddMethodTranslator(nameof(IFormFile.FileName), FileName);
            this.AddMethodTranslator(nameof(IFormFile.Name), FileName);
            this.AddMethodTranslator(nameof(IFormFile.Length), Length);
            this.AddMethodTranslator(nameof(IFormFile.ContentType), ContentType);
        }

        private static void FileName(MethodTranslationContext context)
        {
            context.Translator.Translate(context.Expression.Object, context);
            context.Writer.Write(".name");
        }

        private static void Length(MethodTranslationContext context)
        {
            context.Translator.Translate(context.Expression.Object, context);
            context.Writer.Write(".size");
        }

        private static void ContentType(MethodTranslationContext context)
        {
            context.Translator.Translate(context.Expression.Object, context);
            context.Writer.Write(".type");
        }
    }
}