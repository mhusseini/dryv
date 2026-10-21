using System;
using Dryv.Rules;
using Dryv.Translation;
using Dryv.Validation;

namespace Dryv.Configuration
{
    public class DryvOptions
    {
        public static readonly Type DefaultClientFunctionWriterType = typeof(DryvClientValidationFunctionWriter);
        public static readonly Type DefaultClientValidationSetWriterType = typeof(DryvWindowValidationSetWriter);
        public virtual DryvServiceCollection Annotators { get; } = new(typeof(IDryvRuleAnnotator));
        public Type ClientFunctionWriterType { get; internal set; } = DefaultClientFunctionWriterType;
        public Type ClientValidationSetWriterType { get; internal set; } = DefaultClientValidationSetWriterType;
        public Type TranslatorType { get; internal set; }
        public bool DisableAutomaticValidation { get; set; }
        public bool IncludeModelDataInExceptions { get; set; }
        public Func<object, string> JsonConversion { get; set; }
        public TranslationErrorBehavior TranslationErrorBehavior { get; set; }
        public virtual DryvServiceCollection Translators { get; } = new(typeof(IDryvMethodCallTranslator), typeof(IDryvCustomTranslator));
        public bool DisableParameterInjection { get; set; }
    }
}