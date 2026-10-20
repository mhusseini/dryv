using System.Linq.Expressions;
using System.Reflection;

namespace Dryv.Translation
{
    internal class NullComparisonWriter
    {
        private readonly JavaScriptTranslator translator;

        public NullComparisonWriter(JavaScriptTranslator translator)
        {
            this.translator = translator;
        }

        public bool TryWriteNullComparison(BinaryExpression expression, TranslationContext context, bool negated)
        {
            var operand = GetNullComparisonOperand(expression);

            if (operand == null)
            {
                return false;
            }

            var isNullCheck = (expression.NodeType == ExpressionType.Equal) ^ negated;

            if (operand.Type.GetTypeInfo().IsValueType)
            {
                this.WriteValueTypeNullCheck(operand, isNullCheck, context);
                return true;
            }

            if (isNullCheck)
            {
                context.Writer.Write("!");
            }

            this.WriteOperand(operand, context);
            return true;
        }

        public bool TryWriteValueTypeCoalesce(BinaryExpression expression, TranslationContext context)
        {
            if (expression.NodeType != ExpressionType.Coalesce || !expression.Left.Type.GetTypeInfo().IsValueType)
            {
                return false;
            }

            this.WriteValueTypeNullCheck(expression.Left, true, context);
            context.Writer.Write(" ? ");
            this.WriteOperand(expression.Right, context);
            context.Writer.Write(" : ");
            this.WriteOperand(expression.Left, context);

            return true;
        }

        public static Expression GetNullComparisonOperand(BinaryExpression expression)
        {
            if (expression.NodeType != ExpressionType.Equal && expression.NodeType != ExpressionType.NotEqual)
            {
                return null;
            }

            return IsNullConstant(expression.Right) ? expression.Left
                : IsNullConstant(expression.Left) ? expression.Right
                : null;
        }

        private static bool IsNullConstant(Expression expression)
        {
            while (expression is UnaryExpression { NodeType: ExpressionType.Convert, Method: null } conversion &&
                   conversion.Type.GetTypeInfo().IsValueType)
            {
                expression = conversion.Operand;
            }

            return expression is ConstantExpression { Value: null };
        }

        private void WriteValueTypeNullCheck(Expression operand, bool isNullCheck, TranslationContext context)
        {
            this.WriteOperand(operand, context);
            context.Writer.Write(isNullCheck ? " == null || " : " != null && ");
            this.WriteOperand(operand, context);
            context.Writer.Write(isNullCheck ? " === \"\"" : " !== \"\"");
        }

        private void WriteOperand(Expression operand, TranslationContext context)
        {
            if (!this.translator.TryWriteInjectedExpression(operand, context))
            {
                this.translator.Translate(operand, context);
            }
        }
    }
}
