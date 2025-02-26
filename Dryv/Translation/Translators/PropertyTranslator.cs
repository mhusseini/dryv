using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Dryv.Translation.Translators
{
    public abstract class PropertyTranslator : IDryvCustomTranslator
    {
        private readonly List<RegexAndTranslator> propertyTranslatorsByRegex = new List<RegexAndTranslator>();

        private readonly List<Type> supportedTypes = new List<Type>();

        public int? OrderIndex { get; set; }

        public virtual bool SupportsType(Type type)
        {
            return this.supportedTypes.Find(t => t.IsAssignableFrom(type)) != null;
        }

        public bool TryTranslate(CustomTranslationContext context)
        {
            if (!(context.Expression is MemberExpression memberExpression) ||
                memberExpression.Member.MemberType != MemberTypes.Property &&
                memberExpression.Member.MemberType != MemberTypes.Field ||
                !this.supportedTypes.Contains(memberExpression.Expression?.Type))
            {
                return false;
            }

            var propertyName = memberExpression.Member.Name;
            var translator = this.propertyTranslatorsByRegex
                .Where(i => i.Property.IsMatch(propertyName))
                .Select(i => i.Translator)
                .FirstOrDefault();

            if (translator == null)
            {
                return false;
            }

            if (!context.WhatIfMode)
            {
                translator(context, memberExpression.Expression);
            }

            return true;
        }

        protected void AddPropertyTranslator(string methodName, Action<CustomTranslationContext, Expression> translator)
        {
            this.AddPropertyTranslator(new Regex($"^{methodName}$"), translator);
        }

        protected void AddPropertyTranslator(Regex regex, Action<CustomTranslationContext, Expression> translator)
        {
            this.propertyTranslatorsByRegex.Add(new RegexAndTranslator(regex, translator));
        }

        protected void Supports(Type type)
        {
            this.supportedTypes.Add(type);
        }

        protected void Supports<T>()
        {
            this.Supports(typeof(T));
        }

        private struct RegexAndTranslator
        {
            public RegexAndTranslator(Regex property, Action<CustomTranslationContext, Expression> translator)
            {
                this.Property = property;
                this.Translator = translator;
            }

            public Regex Property { get; }
            public Action<CustomTranslationContext, Expression> Translator { get; }
        }

        public virtual bool? AllowSurroundingBrackets(Expression expression) => null;

    }
}