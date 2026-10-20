using System;
using Dryv.Configuration;

namespace Dryv.Rules
{
    public class DryvTranslatedRule : DryvCompiledRule
    {
        internal DryvTranslatedRule()
        {
        }

        internal DryvTranslatedRule(DryvCompiledRule source) : base(source)
        {
        }

        public string CodeTemplate { get; internal set; }
        public bool IsAsync { get; internal set; }
        public Func<Func<Type, object>, object[], DryvOptions, string> TranslatedValidationExpression { get; internal set; }
        public Exception TranslationError { get; internal set; }
    }
}
