using System.Linq.Expressions;
using Dryv.Configuration;
using Dryv.Rules;

namespace Dryv.Translation.Translators
{
    public class CustomCodeTranslator : MethodCallTranslator, IDryvCustomTranslator
    {
        private readonly DryvOptions options;

        public CustomCodeTranslator(DryvOptions options)
        {
            this.options = options;
            this.Supports<DryvClientCode>();
            this.AddMethodTranslator(nameof(DryvClientCode.Raw), CustomScript);
        }

        public bool? AllowSurroundingBrackets(Expression expression)
        {
            return !TryGetBinaryExpression(expression, out _);
        }

        public bool TryTranslate(CustomTranslationContext context)
        {
            if (!TryGetBinaryExpression(context.Expression, out var binaryExpression))
            {
                return false;
            }

            context.Translator.Translate(binaryExpression.Left, context);
            context.Translator.Translate(binaryExpression.Right, context);

            return true;
        }

        private void CustomScript(MethodTranslationContext context)
        {
            foreach (var script in context.Expression.Arguments)
            {
                context.InjectRuntimeExpression(this.options, script, true);
            }
        }

        private static MethodCallExpression GetDryvClientCodeMethodCall(Expression expression)
        {
            return expression is MethodCallExpression callExpression2 && callExpression2.Method.DeclaringType == typeof(DryvClientCode)
                ? callExpression2
                : null;
        }

        private static bool TryGetBinaryExpression(Expression expression, out BinaryExpression binaryExpression)
        {
            if (expression.NodeType != ExpressionType.Add || !(expression is BinaryExpression b))
            {
                binaryExpression = null;
                return false;
            }

            binaryExpression = b;

            var clientCodeMethodCall = GetDryvClientCodeMethodCall(binaryExpression.Left) ?? GetDryvClientCodeMethodCall(binaryExpression.Right);
            return clientCodeMethodCall != null;
        }
    }
}