using System;
using System.Linq.Expressions;
using System.Reflection;
using Dryv.Extensions;

namespace Dryv.Translation
{
    internal class MemberAccessWriter
    {
        private readonly JavaScriptTranslator translator;

        public MemberAccessWriter(JavaScriptTranslator translator)
        {
            this.translator = translator;
        }

        public void WriteMember(MemberExpression expression, TranslationContext context)
        {
            if (this.translator.TryWriteInjectedExpression(expression, context))
            {
                return;
            }

            if (expression.Member.Name == nameof(Nullable<int>.Value) &&
                expression.Expression != null &&
                Nullable.GetUnderlyingType(expression.Expression.Type) != null)
            {
                this.translator.Translate(expression.Expression, context);
                return;
            }

            if (expression.Expression is ConstantExpression)
            {
                var instance = (expression.Expression as ConstantExpression)?.Value;
                switch (expression.Member)
                {
                    case PropertyInfo property:
                    {
                        var value = property.GetValue(instance);
                        context.Writer.Write(this.translator.TranslateValue(value));
                        break;
                    }
                    case FieldInfo field:
                    {
                        var value = field.GetValue(instance);
                        context.Writer.Write(this.translator.TranslateValue(value));
                        break;
                    }
                }
            }
            else
            {
                if (context.PropertyExpression != null &&
                    expression.Expression.ToString().Contains(context.PropertyExpression.ToString()))
                {
                    var e = expression;

                    switch (e.Expression)
                    {
                        case MemberExpression mex:
                            this.translator.Visit(mex, context);
                            break;

                        case ParameterExpression parameterExpression:
                            this.translator.Visit(parameterExpression, context);
                            break;

                        default:
                            this.translator.Translate(expression.Expression, context);
                            break;
                    }
                }
                else
                {
                    this.translator.Translate(expression.Expression, context);
                }

                context.Writer.Write(".");

                context.Writer.Write(this.translator.FormatIdentifier(expression.Member.Name.ToCamelCase()));
            }
        }
    }
}
