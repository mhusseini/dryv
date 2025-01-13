using System.Linq;
using Dryv.Translation;
using Dryv.Translation.Translators;
using Microsoft.AspNetCore.Http;

namespace Dryv.AspNetCore.Translators
{
    public class FormFileCollectionTranslator : MethodCallTranslator
    {
        public FormFileCollectionTranslator()
        {
            this.Supports<IFormFileCollection>();

            this.AddMethodTranslator(nameof(IFormFileCollection.Count), Count);
            this.AddMethodTranslator(nameof(IFormFileCollection.GetFiles), GetFiles);
            this.AddMethodTranslator(nameof(IFormFileCollection.GetFile), GetFile);
        }

        private static void Count(MethodTranslationContext context)
        {
            context.Translator.Translate(context.Expression.Object, context);
            context.Writer.Write(".length");
        }

        private static void GetFiles(MethodTranslationContext context)
        {
            context.Translator.Translate(context.Expression.Object, context);
            context.Writer.Write(".filter(f => f.name.toLowerCase() === ");
            context.Translator.Translate(context.Expression.Arguments.First(), context);
            context.Writer.Write(")");
        }

        private static void GetFile(MethodTranslationContext context)
        {
            context.Translator.Translate(context.Expression.Object, context);
            context.Writer.Write(".find(f => f.name.toLowerCase() === ");
            context.Translator.Translate(context.Expression.Arguments.First(), context);
            context.Writer.Write(")");
        }
    }
}