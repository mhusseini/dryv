using System.Linq.Expressions;
using Dryv.Translation;
using Dryv.Translation.Translators;
using Microsoft.AspNetCore.Http;

namespace Dryv.AspNetCore.Translators
{
    public class FormFileTranslator : PropertyTranslator
    {
        public FormFileTranslator()
        {
            this.Supports<IFormFile>();
            this.AddPropertyTranslator(nameof(IFormFile.FileName), FileName);
            this.AddPropertyTranslator(nameof(IFormFile.Name), FileName);
            this.AddPropertyTranslator(nameof(IFormFile.Length), Length);
            this.AddPropertyTranslator(nameof(IFormFile.ContentType), ContentType);
        }

        private static void FileName(CustomTranslationContext context, Expression objectExpression)
        {
            context.Translator.Translate(objectExpression, context);
            context.Writer.Write(".name");
        }

        private static void Length(CustomTranslationContext context, Expression objectExpression)
        {
            context.Translator.Translate(objectExpression, context);
            context.Writer.Write(".size");
        }

        private static void ContentType(CustomTranslationContext context, Expression objectExpression)
        {
            context.Translator.Translate(objectExpression, context);
            context.Writer.Write(".type");
        }
    }
}