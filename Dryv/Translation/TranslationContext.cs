using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Dryv.Rules;

namespace Dryv.Translation
{
    [DebuggerDisplay("{" + nameof(DebugView) + "}")]
    public class TranslationContext
    {
        internal class SessionCounter
        {
            private int value;
            public int Next() => ++value;
        }

        private class SharedAsyncFlag
        {
            public bool Value { get; set; }
        }

        private SharedAsyncFlag asyncFlag = new();
        public Dictionary<object, object> CustomData { get; private set; } = new();
        private string DebugView => this.StringBuilder?.Length > 0 ? this.StringBuilder.ToString() : "PARENT: " + this.ParentContext?.DebugView;
        public List<Func<Expression, TranslationContext, bool>> DynamicTranslation { get; private set; } = new();
        public string Group { get; set; }

        public bool IsAsync
        {
            get => this.asyncFlag.Value;
            set => this.asyncFlag.Value = value;
        }

        public Type ModelType { get; set; }
        public IDictionary<string, InjectedExpression> InjectedExpressions { get; set; }
        public IList<Type> InjectedServiceTypes { get; set; }
        public TranslationContext ParentContext { get; set; }
        public Expression PropertyExpression { get; set; }
        public DryvRuleDefinition Rule { get; set; }
        public ITranslator Translator { get; set; }
        public bool WhatIfMode { get; set; }
        public StringWriter Writer { get; set; }
        internal StringBuilder StringBuilder { get; set; }
        internal SessionCounter ParameterCounter { get; private set; } = new();

        public virtual T Clone<T>(StringBuilder sb = null, bool shareAsyncFlag = true)
            where T : TranslationContext, new()
        {
            return new T
            {
                ParentContext = this,
                Group = this.Group,
                ModelType = this.ModelType,
                InjectedExpressions = this.InjectedExpressions,
                InjectedServiceTypes = this.InjectedServiceTypes,
                PropertyExpression = this.PropertyExpression,
                Writer = sb == null ? this.Writer : new StringWriter(sb),
                StringBuilder = sb,
                WhatIfMode = this.WhatIfMode,
                Rule = this.Rule,
                Translator = this.Translator,
                CustomData = this.CustomData,
                DynamicTranslation = this.DynamicTranslation,
                ParameterCounter = this.ParameterCounter,
                asyncFlag = shareAsyncFlag ? this.asyncFlag : new SharedAsyncFlag(),
            };
        }
    }
}