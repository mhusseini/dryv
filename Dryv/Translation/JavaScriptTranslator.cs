using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Dryv.Configuration;
using Dryv.Extensions;
using Dryv.Reflection;
using Dryv.Rules;
using Dryv.Translation.Translators;
using Dryv.Translation.Visitors;

namespace Dryv.Translation
{
    public class JavaScriptTranslator : Translator
    {
        private static readonly MethodInfo TranslateValueMethodInfo = typeof(JavaScriptTranslator).GetMethod(nameof(TranslateValue), typeof(object), typeof(DryvOptions));

        private readonly TranslationCompiler translationCompiler = new();

        private static readonly Dictionary<ExpressionType, string> Terminals = new()
        {
            //[ExpressionType.ArrayIndex] = "",
            //[ExpressionType.ArrayLength] = "",
            //[ExpressionType.Block] = "",
            //[ExpressionType.Call] = "",
            //[ExpressionType.Constant] = "",
            //[ExpressionType.Convert] = "",
            //[ExpressionType.ConvertChecked] = "",
            //[ExpressionType.DebugInfo] = "",
            //[ExpressionType.Default] = "",
            //[ExpressionType.Dynamic] = "",
            //[ExpressionType.Extension] = "",
            //[ExpressionType.Goto] = "",
            //[ExpressionType.Index] = "",
            //[ExpressionType.Invoke] = "",
            //[ExpressionType.Label] = "",
            //[ExpressionType.Lambda] = "",
            //[ExpressionType.ListInit] = "",
            //[ExpressionType.Loop] = "",
            //[ExpressionType.MemberAccess] = "",
            //[ExpressionType.MemberInit] = "",
            //[ExpressionType.New] = "",
            //[ExpressionType.NewArrayBounds] = "",
            //[ExpressionType.NewArrayInit] = "",
            //[ExpressionType.OnesComplement] = "",
            //[ExpressionType.Parameter] = "",
            //[ExpressionType.RuntimeVariables] = "",
            //[ExpressionType.Switch] = "",
            //[ExpressionType.Throw] = "throw",
            //[ExpressionType.Try] = "",
            //[ExpressionType.TypeAs] = "",
            //[ExpressionType.TypeEqual] = "",
            //[ExpressionType.Unbox] = "",
            [ExpressionType.Add] = "+",
            [ExpressionType.AddAssign] = "+=",
            [ExpressionType.AddAssignChecked] = "+=",
            [ExpressionType.AddChecked] = "+",
            [ExpressionType.And] = "&&",
            [ExpressionType.AndAlso] = "&&",
            [ExpressionType.AndAssign] = "&=",
            [ExpressionType.Assign] = "=",
            [ExpressionType.Coalesce] = "||",
            //[ExpressionType.Conditional] = "",
            [ExpressionType.Decrement] = "--",
            [ExpressionType.Divide] = "/",
            [ExpressionType.DivideAssign] = "/=",
            [ExpressionType.Equal] = "===",
            [ExpressionType.ExclusiveOr] = "||",
            [ExpressionType.ExclusiveOrAssign] = "|=",
            [ExpressionType.GreaterThan] = ">",
            [ExpressionType.GreaterThanOrEqual] = ">=",
            [ExpressionType.Increment] = "++",
            [ExpressionType.IsFalse] = "!== false",
            [ExpressionType.IsTrue] = "!== true",
            [ExpressionType.LeftShift] = "<<",
            [ExpressionType.LeftShiftAssign] = "<<=",
            [ExpressionType.LessThan] = "<",
            [ExpressionType.LessThanOrEqual] = "<=",
            [ExpressionType.Modulo] = "%",
            [ExpressionType.ModuloAssign] = "%=",
            [ExpressionType.Multiply] = "*",
            [ExpressionType.MultiplyAssign] = "*=",
            [ExpressionType.MultiplyAssignChecked] = "*=",
            [ExpressionType.MultiplyChecked] = "*",
            [ExpressionType.Negate] = "-",
            [ExpressionType.NegateChecked] = "-",
            [ExpressionType.Not] = "!",
            [ExpressionType.NotEqual] = "!=",
            [ExpressionType.Or] = "||",
            [ExpressionType.OrAssign] = "|=",
            [ExpressionType.OrElse] = "||",
            [ExpressionType.PostDecrementAssign] = "--",
            [ExpressionType.PostIncrementAssign] = "++",
            [ExpressionType.Power] = "**",
            [ExpressionType.PowerAssign] = "**=",
            [ExpressionType.PreDecrementAssign] = "--",
            [ExpressionType.PreIncrementAssign] = "++",
            [ExpressionType.Quote] = "\"",
            [ExpressionType.RightShift] = ">>",
            [ExpressionType.RightShiftAssign] = ">>=",
            [ExpressionType.Subtract] = "-",
            [ExpressionType.SubtractAssign] = "-=",
            [ExpressionType.SubtractAssignChecked] = "-=",
            [ExpressionType.SubtractChecked] = "-",
            [ExpressionType.TypeIs] = "is",
            [ExpressionType.UnaryPlus] = "++",
        };

        private readonly IReadOnlyCollection<IDryvCustomTranslator> customTranslators;
        private readonly IReadOnlyCollection<IDryvMethodCallTranslator> methodCallTranslators;
        private readonly NullComparisonWriter nullComparisonWriter;
        private readonly AsyncPromiseWriter asyncPromiseWriter;
        private readonly MemberAccessWriter memberAccessWriter;

        public JavaScriptTranslator(IReadOnlyCollection<IDryvCustomTranslator> customTranslators, IReadOnlyCollection<IDryvMethodCallTranslator> methodCallTranslators, DryvOptions options) : base(options)
        {
            this.customTranslators = customTranslators;
            this.methodCallTranslators = methodCallTranslators;
            this.nullComparisonWriter = new NullComparisonWriter(this);
            this.asyncPromiseWriter = new AsyncPromiseWriter(this);
            this.memberAccessWriter = new MemberAccessWriter(this);
        }

        public bool UseLowercaseMembers { get; set; }

        public override string FormatIdentifier(string name)
        {
            return this.UseLowercaseMembers
                ? name.Length == 1
                    ? name.ToLower()
                    : name.Substring(0, 1).ToLower() + name.Substring(1)
                : name;
        }

        public override TranslationResult Translate(Expression expression, MemberExpression propertyExpression, DryvRuleDefinition rule)
        {
            var result = this.GenerateJavaScriptCode(expression, propertyExpression, rule);
            var translationResult = this.translationCompiler.GenerateTranslationDelegate(result.Code, result.OptionDelegates, result.ServiceTypes, TranslateValueMethodInfo);
            translationResult.IsAsync = result.IsAsync;
            return translationResult;
        }

        public override void Translate(Expression expression, TranslationContext context, bool negated = false)
        {
            if (negated && this.TryWriteNegation(expression, context))
            {
                return;
            }

            var needsBrackets = this.GetNeedsBrackets(expression);

            if (needsBrackets)
            {
                context.Writer.Write("(");
            }

            var context2 = context.Clone<CustomTranslationContext>();
            context2.Translator = this;
            context2.Expression = expression;
            context2.Negated = negated;

            if (!this.customTranslators.Any(t => t.TryTranslate(context2)) &&
                !context.DynamicTranslation.Any(t => t(expression, context2)))
            {
                switch (expression)
                {
                    case BinaryExpression e: this.Visit(e, context2, negated); break;
                    case BlockExpression e: this.Visit(e, context2, negated); break;
                    case ConditionalExpression e: this.Visit(e, context2, negated); break;
                    case ConstantExpression e: this.Visit(e, context2, negated); break;
                    case DebugInfoExpression e: this.Visit(e, context2, negated); break;
                    case DefaultExpression e: this.Visit(e, context2, negated); break;
                    case DynamicExpression e: this.Visit(e, context2, negated); break;
                    case GotoExpression e: this.Visit(e, context2, negated); break;
                    case IndexExpression e: this.Visit(e, context2, negated); break;
                    case InvocationExpression e: this.Visit(e, context2, negated); break;
                    case LabelExpression e: this.Visit(e, context2, negated); break;
                    case LambdaExpression e: this.Visit(e, context2, negated); break;
                    case ListInitExpression e: this.Visit(e, context2, negated); break;
                    case LoopExpression e: this.Visit(e, context2, negated); break;
                    case MemberExpression e: this.Visit(e, context2, negated); break;
                    case MemberInitExpression e: this.Visit(e, context2, negated); break;
                    case MethodCallExpression e: this.Visit(e, context2, negated); break;
                    case NewArrayExpression e: this.Visit(e, context2, negated); break;
                    case NewExpression e: this.Visit(e, context2, negated); break;
                    case ParameterExpression e: this.Visit(e, context2, negated); break;
                    case RuntimeVariablesExpression e: this.Visit(e, context2, negated); break;
                    case SwitchExpression e: this.Visit(e, context2, negated); break;
                    case TryExpression e: this.Visit(e, context2, negated); break;
                    case TypeBinaryExpression e: this.Visit(e, context2, negated); break;
                    case UnaryExpression e: this.Visit(e, context2, negated); break;
                    default: throw new NotSupportedException($"Expression type '{expression.GetType().Name}' is not supported.");
                }
            }
            else if (context2.IsAsync)
            {
                context.IsAsync = true;
            }

            if (needsBrackets)
            {
                context.Writer.Write(") ");
            }
        }

        public override string TranslateValue(object value)
        {
            return TranslateValue(value, this.Options);
        }

        public static string TranslateValue(object value, DryvOptions options)
        {
            return JavaScriptHelper.TranslateValue(value) ?? value switch
            {
                DryvValidationResult result => TranslateValidationResultObject(result, options),
                _ => (options.JsonConversion == null ? value.ToString() : options.JsonConversion(value))
            };
        }

        public override bool TryWriteTerminal(Expression expression, TextWriter writer)
        {
            if (!Terminals.TryGetValue(expression.NodeType, out var terminal))
            {
                return false;
            }

            writer.Write(terminal);
            return true;
        }

        public override void Visit(BinaryExpression expression, TranslationContext context, bool negated = false, bool leftOnly = false)
        {
            if (TryWriteInjectedExpression(expression, context))
            {
                return;
            }

            if (this.nullComparisonWriter.TryWriteNullComparison(expression, context, negated))
            {
                return;
            }

            if (this.nullComparisonWriter.TryWriteValueTypeCoalesce(expression, context))
            {
                return;
            }

            if (!TryWriteInjectedExpression(expression.Left, context))
            {
                this.Translate(expression.Left, context);
            }

            if (!TryWriteTerminal(expression, context.Writer))
            {
                throw expression.Method != null
                    ? new DryvMethodNotSupportedException(expression)
                    : new DryvExpressionNotSupportedException(expression);
            }

            if (!leftOnly && !TryWriteInjectedExpression(expression.Right, context))
            {
                this.Translate(expression.Right, context);
            }
        }

        protected override void Visit(BlockExpression expression, TranslationContext context, bool negated = false)
        {
            if (TryWriteInjectedExpression(expression, context))
            {
                return;
            }

            if (expression.Variables.Any())
            {
                context.Writer.Write("var ");
                var sep = string.Empty;

                foreach (var variable in expression.Variables)
                {
                    context.Writer.Write(sep);
                    context.Writer.Write(this.FormatIdentifier(variable.Name));
                    sep = ", ";
                }

                context.Writer.Write(";");
            }

            base.Visit(expression, context, negated);
        }

        public override void Visit(ConstantExpression expression, TranslationContext context, bool negated = false)
        {
            var text = this.TranslateValue(expression.Value);

            context.Writer.Write(text);
        }

        public override void Visit(DefaultExpression expression, TranslationContext context, bool negated = false)
        {
            var value = GetDefaultValue(expression.Type);
            var text = this.TranslateValue(value);

            context.Writer.Write(text);
        }

        public override void Visit(DynamicExpression expression, TranslationContext context, bool negated = false)
        {
            throw new NotSupportedException();
        }

        public override void Visit(GotoExpression expression, TranslationContext context, bool negated = false)
        {
            throw new NotSupportedException();
        }

        public override void Visit(IndexExpression expression, TranslationContext context, bool negated = false)
        {
            if (expression.Arguments.Count > 1)
            {
                throw new NotSupportedException("JavaScript does not support indexers with more than one argument.");
            }

            if (TryWriteInjectedExpression(expression, context))
            {
                return;
            }

            this.Translate(expression.Object, context);
            context.Writer.Write("[");
            this.Translate(expression.Arguments.First(), context);
            context.Writer.Write("]");
        }

        public override void Visit(InvocationExpression expression, TranslationContext context, bool negated = false)
        {
            if (expression.Expression is MemberExpression)
            {
                throw new DryvExpressionNotSupportedException(expression);
            }

            if (TryWriteInjectedExpression(expression, context))
            {
                return;
            }

            this.Translate(expression.Expression, context);
            context.Writer.Write("(");
            MethodCallTranslator.WriteArguments(this, expression.Arguments, context);
            context.Writer.Write(")");
        }

        public override void Visit(LabelExpression expression, TranslationContext context, bool negated = false)
        {
            throw new NotSupportedException();
        }

        public override void Visit(LambdaExpression expression, TranslationContext context, bool negated = false)
        {
            var parameters = new[]
            {
                expression.Parameters
                    .Where(p => p.Type == context.ModelType)
                    .Select(p => this.FormatIdentifier(p.Name))
                    .FirstOrDefault(),
                "$ctx"
            }.Where(p => !string.IsNullOrWhiteSpace(p));

            context.Writer.Write("function(");
            context.Writer.Write(string.Join(", ", parameters));
            context.Writer.Write(") {");
            context.Writer.Write("return ");

            this.Translate(expression.Body, context);

            context.Writer.Write("}");
        }

        public override void Visit(ConditionalExpression expression, TranslationContext context, bool negated = false)
        {
            var finder = new AsyncBinaryFinder(this.methodCallTranslators, context);
            finder.Visit(expression.Test);

            if (finder.AsyncPath.Any())
            {
                var test = AsyncBinarySwitcher.Modify(expression.Test, finder.AsyncPath);

                var asyncExpression = test is BinaryExpression chain
                    ? this.asyncPromiseWriter.TranslateAsyncBooleanChain(chain, context, finder.AsyncPath)
                    : expression.Test;

                var asyncFinder = new AsyncMethodCallModifier(this, context);
                var body = asyncFinder.ApplyPromises(asyncExpression);

                this.asyncPromiseWriter.WriteAsyncPromiseChain(asyncFinder.AsyncCalls, context, () =>
                {
                    this.Translate(body, context);

                    context.Writer.Write(" ? ");
                    this.Translate(expression.IfTrue, context);
                    context.Writer.Write(" : ");
                    this.Translate(expression.IfFalse, context);
                });
            }
            else
            {
                this.Translate(expression.Test, context);

                context.Writer.Write(" ? ");
                this.Translate(expression.IfTrue, context);
                context.Writer.Write(" : ");
                this.Translate(expression.IfFalse, context);
            }
        }

        public override void Visit(ListInitExpression expression, TranslationContext context, bool negated = false)
        {
            context.Writer.Write("= [");
            var sep = string.Empty;

            foreach (var initializer in expression.Initializers)
            {
                context.Writer.Write(sep);
                foreach (var argument in initializer.Arguments)
                {
                    this.Translate(argument, context);
                }

                sep = ", ";
            }

            context.Writer.Write("]");
        }

        public override void Visit(LoopExpression expression, TranslationContext context, bool negated = false)
        {
            throw new NotImplementedException();
        }

        public override void Visit(MemberExpression expression, TranslationContext context, bool negated = false)
        {
            var asyncFinder = new AsyncMethodCallModifier(this, context);
            var body = asyncFinder.ApplyPromises(expression);

            this.asyncPromiseWriter.WriteAsyncPromiseChain(asyncFinder.AsyncCalls, context, () =>
            {
                if (negated)
                {
                    context.Writer.Write("!(");
                }

                if (asyncFinder.AsyncCalls.Any())
                {
                    this.Translate(body, context);
                }
                else
                {
                    this.memberAccessWriter.WriteMember(expression, context);
                }

                if (negated)
                {
                    context.Writer.Write(")");
                }
            });
        }

        public override void Visit(MemberInitExpression expression, TranslationContext context, bool negated = false)
        {
            this.Visit(expression.NewExpression, context);
        }

        public override void Visit(MethodCallExpression expression, TranslationContext context, bool negated = false)
        {
            // .NET 10 compiles e.g. array.Contains() using ReadOnlySpan<T> with an
            // implicit conversion operator in the expression tree. Type conversions
            // are meaningless in JavaScript, so we just translate the operand.
            if (expression.Method.IsSpecialName &&
                (expression.Method.Name == "op_Implicit" || expression.Method.Name == "op_Explicit") &&
                expression.Arguments.Count == 1)
            {
                this.Translate(expression.Arguments[0], context, negated);
                return;
            }

            if (TryWriteInjectedExpression(expression, context))
            {
                return;
            }

            var asyncCalls = new Dictionary<StringBuilder, ParameterExpression>();
            var newArguments = new List<Expression>();

            foreach (var argument in expression.Arguments)
            {
                var asyncFinder = new AsyncMethodCallModifier(this, context);
                var newArgument = asyncFinder.ApplyPromises(argument);

                newArguments.Add(newArgument);
                asyncCalls.AddRange(asyncFinder.AsyncCalls);
            }

            var newExpression = Expression.Call(expression.Object, expression.Method, newArguments);

            this.asyncPromiseWriter.WriteAsyncPromiseChain(asyncCalls, context, () =>
            {
                this.TranslateMethodCall(newExpression, context, negated);
            });
        }

        public override void Visit(NewArrayExpression expression, TranslationContext context, bool negated = false)
        {
            context.Writer.Write("[");
            foreach (var child in expression.Expressions)
            {
                this.Translate(child, context);
            }

            context.Writer.Write("]");
        }

        public override void Visit(NewExpression expression, TranslationContext context, bool negated = false)
        {
            context.Writer.Write("{}");
        }

        public override void Visit(ParameterExpression expression, TranslationContext context, bool negated = false)
        {
            context.Writer.Write(expression.Name);
        }

        public override void Visit(RuntimeVariablesExpression expression, TranslationContext context, bool negated = false)
        {
            throw new NotImplementedException();
        }

        public override void Visit(SwitchExpression expression, TranslationContext context, bool negated = false)
        {
            context.Writer.Write("switch(");
            if (!TryWriteInjectedExpression(expression.SwitchValue, context))
            {
                this.Translate(expression.SwitchValue, context);
            }

            context.Writer.WriteLine("){");
            foreach (var expressionCase in expression.Cases)
            {
                foreach (var testCase in expressionCase.TestValues)
                {
                    context.Writer.Write("case ");
                    this.Translate(testCase, context);
                    context.Writer.WriteLine(":");
                }

                context.Writer.WriteLine("{");
                this.Translate(expressionCase.Body, context);
                context.Writer.WriteLine();
                context.Writer.WriteLine("break;");
                context.Writer.WriteLine("}");
            }

            context.Writer.Write("}");
        }

        public override void Visit(TryExpression expression, TranslationContext context, bool negated = false)
        {
            throw new NotImplementedException();
        }

        public override void Visit(TypeBinaryExpression expression, TranslationContext context, bool negated = false)
        {
            throw new DryvExpressionNotSupportedException(expression);
        }

        public override void Visit(UnaryExpression expression, TranslationContext context, bool negated = false)
        {
            if (TryWriteInjectedExpression(expression, context))
            {
                return;
            }

            var negatedExpression = false;

            switch (expression.NodeType)
            {
                case ExpressionType.ArrayLength:
                    this.Translate(expression.Operand, context);
                    context.Writer.Write(".length");
                    return;

                case ExpressionType.Not:
                    negatedExpression = true;
                    break;

                case ExpressionType.Convert:
                    if (!(expression.Operand is ConstantExpression constant) || constant.Value == null)
                    {
                        break;
                    }

                    if (expression.Type == typeof(DryvValidationResult))
                    {
                        context.Writer.Write("{ type:\"error\", text:");
                        this.Translate(expression.Operand, context);
                        context.Writer.Write(", group: ");
                        context.Writer.Write(JavaScriptHelper.TranslateValue(context.Group));
                        context.Writer.Write("}");

                        return;
                    }

                    if (expression.Type.IsEnumOrNullableEnum(out var enumType))
                    {
                        var value = expression.GetValue();
                        if (value == null)
                        {
                            context.Writer.Write(this.TranslateValue(null));
                        }
                        else
                        {
                            var enumValue = Enum.ToObject(enumType, value);
                            context.Writer.Write(this.TranslateValue(enumValue));
                        }

                        return;
                    }

                    break;
            }

            if (!negatedExpression)
            {
                TryWriteTerminal(expression, context.Writer);
            }

            this.Translate(expression.Operand, context, negatedExpression);
        }

        internal bool TryWriteInjectedExpression(Expression expression, TranslationContext context)
        {
            var parameters = ExpressionInjectionHelper.GetInjectionParameters(this.Options, expression, context);
            return context.InjectRuntimeExpression(this.Options, expression, parameters);
        }

        private bool TryWriteNegation(Expression expression, TranslationContext context)
        {
            var injected = new StringBuilder();

            if (this.TryWriteInjectedExpression(expression, context.Clone<TranslationContext>(injected)))
            {
                context.Writer.Write("!");
                context.Writer.Write(injected.ToString());
                return true;
            }

            if (expression is MemberExpression ||
                expression is MethodCallExpression ||
                expression is BinaryExpression binary && NullComparisonWriter.GetNullComparisonOperand(binary) != null)
            {
                return false;
            }

            var addBrackets = !this.GetNeedsBrackets(expression);

            context.Writer.Write(addBrackets ? "!(" : "!");
            this.Translate(expression, context);

            if (addBrackets)
            {
                context.Writer.Write(")");
            }

            return true;
        }

        private bool GetNeedsBrackets(Expression expression)
        {
            return expression switch
            {
                BinaryExpression _ => true,
                ConstantExpression _ => false,
                ParameterExpression _ => false,
                MethodCallExpression _ => false,
                MemberExpression _ => false,
                UnaryExpression _ => false,
                LambdaExpression _ => false,
                _ => this.customTranslators.All(t => t.AllowSurroundingBrackets(expression) != false)
            };
        }

        private void TranslateMethodCall(MethodCallExpression expression, TranslationContext context, bool negated)
        {
            var objectType = expression.Object?.Type ?? expression.Method.DeclaringType;

            if (objectType == typeof(MemoryExtensions))
            {
                objectType = typeof(Enumerable);
            }
            var context2 = context.Clone<MethodTranslationContext>();
            context2.Translator = this;
            context2.Expression = expression;
            context2.Negated = negated;

            if (this.methodCallTranslators
                .Where(t => t.SupportsType(objectType))
                .Any(t => t.Translate(context2)))
            {
                return;
            }

            throw new DryvMethodNotSupportedException(expression);
        }

        private GeneratedJavaScriptCode GenerateJavaScriptCode(
            Expression expression,
            MemberExpression propertyExpression,
            DryvRuleDefinition rule)
        {
            // Find all service types used in the validation expression.
            var serviceTypes = ((LambdaExpression)expression).GetInjectedServiceTypes();
            var injectedServiceTypes = serviceTypes.ToList();
            if (this.Options.DisableParameterInjection && injectedServiceTypes.Contains(typeof(DryvParameters)))
            {
                injectedServiceTypes.Remove(typeof(DryvParameters));
            }

            // Collect delegates that use options from withing the validation expression.
            var optionDelegates = new Dictionary<int, InjectedExpression>();
            var sb = new StringBuilder();

            using var writer = new StringWriter(sb);
            var context = new TranslationContext
            {
                InjectedServiceTypes = injectedServiceTypes,
                Writer = writer,
                InjectedExpressions = optionDelegates,
                ModelType = rule.ModelType,
                PropertyExpression = propertyExpression?.Expression,
                Group = rule.Group,
                StringBuilder = sb,
                Rule = rule,
            };

            expression = new NullableMemberModifier().Visit(expression);
            expression = new EnumComparisionModifier().Visit(expression);
            expression = new ConditionConversionModifier().Visit(expression);
            this.Translate(expression, context);

            return new GeneratedJavaScriptCode
            (
                sb.ToString(),
                serviceTypes,
                context.InjectedExpressions.Values.ToList(),
                context.IsAsync
            );
        }

        private static string TranslateValidationResultObject(DryvValidationResult result, DryvOptions options)
        {
            if (result.Type == DryvResultType.Success)
            {
                return "null";
            }

            var sb = new StringBuilder("{");

            if (!string.IsNullOrWhiteSpace(result.Group))
            {
                sb.Append("group:\"");
                sb.Append(result.Group);
                sb.Append("\",");
            }

            sb.Append("text:\"");
            sb.Append(result.Text);
            sb.Append("\",");
            sb.Append("type:\"");
            sb.Append(result.Type.ToString().ToLower());
            sb.Append("\"");
            if (result.Data != null)
            {
                sb.Append("data: ");
                sb.Append(options.JsonConversion(result.Data));
            }

            sb.Append("}");

            return sb.ToString();
        }

        private readonly struct GeneratedJavaScriptCode(
            string code,
            IList<Type> serviceTypes,
            IList<InjectedExpression> optionDelegates,
            bool isAsync)
        {
            public string Code { get; } = code;
            public bool IsAsync { get; } = isAsync;
            public IList<InjectedExpression> OptionDelegates { get; } = optionDelegates;
            public IList<Type> ServiceTypes { get; } = serviceTypes;
        }
    }
}