using System;

namespace Dryv.Rules
{
    public class DryvCompiledRule : DryvRuleDefinition
    {
        internal DryvCompiledRule()
        {
        }

        public Func<object[], object> CompiledEnablingExpression { get; internal set; }
        public Func<object, object[], object> CompiledValidationExpression { get; internal set; }
        public TranslationOutput Translation { get; internal set; }

        internal DryvCompiledRule WithTranslation(TranslationOutput translation, Type[] serviceTypes = null)
        {
            var clone = (DryvCompiledRule)MemberwiseClone();
            clone.Translation = translation;
            if (serviceTypes != null)
                clone.ServiceTypes = serviceTypes;
            return clone;
        }
    }
}