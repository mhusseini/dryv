using System;
using Dryv.Configuration;
using Dryv.Rules;
using Dryv.Translation;

namespace Dryv.AspNetCore.Internal
{
    internal class DryvMvcCoreOptions : DryvOptions
    {
        public DryvMvcCoreOptions()
        {
        }

        public DryvMvcCoreOptions(Action<Type> typeAdded)
        {
            this.Translators = new RegisteringDryvServiceCollection(typeAdded, typeof(IDryvMethodCallTranslator), typeof(IDryvCustomTranslator));
            this.Annotators = new RegisteringDryvServiceCollection(typeAdded, typeof(IDryvRuleAnnotator));
        }

        public override DryvServiceCollection Annotators { get; }
        public override DryvServiceCollection Translators { get; }
    }
}