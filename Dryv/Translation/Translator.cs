using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Dryv.Configuration;
using Dryv.Rules;

namespace Dryv.Translation
{
    public abstract class Translator(DryvOptions options) : ITranslator
    {
        protected DryvOptions Options { get; } = options;

        public virtual string FormatIdentifier(string name)
        {
            return name;
        }

        public abstract TranslationResult Translate(Expression expression, MemberExpression propertyExpression, DryvRuleDefinition rule);

        public virtual void Translate(Expression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual string TranslateValue(object value)
        {
            return value?.ToString();
        }

        public abstract bool TryWriteTerminal(Expression expression, TextWriter writer);

        public virtual void Visit(BinaryExpression expression, TranslationContext context, bool negated = false, bool leftOnly = false)
        {
        }

        protected virtual void Visit(BlockExpression expression, TranslationContext context, bool negated = false)
        {
            foreach (var child in expression.Expressions)
            {
                this.Translate(child, context);
            }
        }

        public virtual void Visit(ConditionalExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(ConstantExpression expression, TranslationContext context, bool negated = false)
        {
        }

        protected virtual void Visit(DebugInfoExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(DefaultExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(DynamicExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(GotoExpression expression, TranslationContext context, bool negated = false)
        {
        }

        //public virtual void Visit(IDynamicExpression expression, TranslationContext context, bool negated = false)
        //{
        //}

        public virtual void Visit(IndexExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(InvocationExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(LabelExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(LambdaExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(ListInitExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(LoopExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(MemberExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(MemberInitExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(MethodCallExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(NewArrayExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(NewExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(ParameterExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(RuntimeVariablesExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(SwitchExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(TryExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(TypeBinaryExpression expression, TranslationContext context, bool negated = false)
        {
        }

        public virtual void Visit(UnaryExpression expression, TranslationContext context, bool negated = false)
        {
        }

        protected static object GetDefaultValue(Type type)
        {
            if (type == typeof(void))
            {
                throw new InvalidOperationException("Cannot get a default value for type 'void'.");
            }
            return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}