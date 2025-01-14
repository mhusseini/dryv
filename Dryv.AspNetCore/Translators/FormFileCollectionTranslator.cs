using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dryv.Translation;
using Dryv.Translation.Translators;
using Microsoft.AspNetCore.Http;

namespace Dryv.AspNetCore.Translators
{
    public class FormFileCollectionTranslator : MethodCallTranslator, IDryvCustomTranslator
    {
        public FormFileCollectionTranslator()
        {
            this.Supports<IFormFileCollection>();

            this.AddMethodTranslator(nameof(IFormFileCollection.GetFiles), GetFiles);
            this.AddMethodTranslator(nameof(IFormFileCollection.GetFile), GetFile);
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

        public bool? AllowSurroundingBrackets(Expression expression) => null;

        public bool TryTranslate(CustomTranslationContext context)
        {
            if (!(context.Expression is MemberExpression memberExpression) ||
                memberExpression.Member.MemberType != MemberTypes.Property ||
                memberExpression.Member.Name != nameof(IFormFileCollection.Count) ||
                memberExpression.Expression?.Type != typeof(IFormFileCollection))
            {
                return false;
            }

            context.Translator.Translate(memberExpression.Expression, context);
            context.Writer.Write(".length");
            return true;
        }
    }
}