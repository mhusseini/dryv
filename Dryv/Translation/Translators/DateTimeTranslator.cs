using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using Dryv.Configuration;

namespace Dryv.Translation.Translators
{
    public class DateTimeTranslator : IDryvCustomTranslator
    {
        private readonly DryvOptions options;
        private static readonly Expression<Func<string>> CultureNameExpression = () => CultureInfo.CurrentUICulture.Name;
        private static readonly Expression<Func<string>> DateTimeOffsetFormatExpression = () => MomentJsFormatConverter.ConvertFormat($"{CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentUICulture.DateTimeFormat.LongTimePattern} zzz");
        private static readonly Expression<Func<string>> DateTimeFormatExpression = () => MomentJsFormatConverter.ConvertFormat($"{CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern} {CultureInfo.CurrentUICulture.DateTimeFormat.LongTimePattern}");

        private readonly HashSet<Type> dateTimeTypes = new HashSet<Type>()
        {
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(DateTime?),
            typeof(DateTimeOffset?)
        };

        public DateTimeTranslator(DryvOptions options)
        {
            this.options = options;
        }

        public int? OrderIndex { get; set; }

        public bool? AllowSurroundingBrackets(Expression expression)
        {
            return true;
        }

        public bool TryTranslate(CustomTranslationContext context)
        {
            if (ExpressionInjectionHelper.GetInjectionParameters(this.options, context.Expression, context) != null ||
                !(context.Expression is BinaryExpression binary) ||
                binary.Left is ConstantExpression { Value: null } ||
                binary.Right is ConstantExpression { Value: null } ||
                !dateTimeTypes.Contains(binary.Left.Type) && !dateTimeTypes.Contains(binary.Right.Type))
            {
                return false;
            }

            TranslateDate(context, binary.Left);
            context.Writer.Write(" ");
            context.Translator.TryWriteTerminal(context.Expression, context.Writer);
            context.Writer.Write(" ");
            TranslateDate(context, binary.Right);

            return true;
        }

        private static void TranslateDate(TranslationContext context, Expression node)
        {
            var format = node.Type == typeof(DateTimeOffset) || node.Type == typeof(DateTimeOffset?)
                ? DateTimeOffsetFormatExpression
                : DateTimeFormatExpression;

            context.Writer.Write("$ctx.dryv.parseDate(");
            context.Translator.Translate(node, context);
            context.Writer.Write(",");
            context.Translator.Translate(CultureNameExpression.Body, context);
            context.Writer.Write(",");
            context.Translator.Translate(format.Body, context);
            context.Writer.Write(")");
        }
    }
}