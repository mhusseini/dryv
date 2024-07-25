using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dryv.Validation;

namespace Dryv.AspNetCore
{
    public class DryvClientWriter
    {
        private readonly DryvTranslator translator;
        private readonly IDryvClientValidationFunctionWriter functionWriter;
        private readonly IDryvClientValidationSetWriter setWriter;

        public DryvClientWriter(DryvTranslator translator, IDryvClientValidationFunctionWriter functionWriter, IDryvClientValidationSetWriter setWriter)
        {
            this.translator = translator ?? throw new ArgumentNullException(nameof(translator));
            this.functionWriter = functionWriter ?? throw new ArgumentNullException(nameof(functionWriter));
            this.setWriter = setWriter ?? throw new ArgumentNullException(nameof(setWriter));
        }

        public async Task<Action<TextWriter>> WriteDryvValidation<TModel>(string validationSetName, Func<Type, object> serviceProvider, IReadOnlyDictionary<string, object> parameters)
        {
            var translation = await this.translator.TranslateValidationRules(typeof(TModel), serviceProvider, parameters);
            var ruleParameters = translation.Parameters;
            var validators = translation.ValidationFunctions.ToDictionary(i => i.Key, i => this.functionWriter.GetValidationFunction(i.Value));
            var disablers = translation.DisablingFunctions.ToDictionary(i => i.Key, i => this.functionWriter.GetValidationFunction(i.Value));

            return writer =>
            {
                this.setWriter.WriteBegin(writer);
                this.setWriter.WriteValidationSet(writer, validationSetName, validators, disablers, ruleParameters);
                this.setWriter.WriteEnd(writer);
            };
        }

        public async Task<Action<TextWriter>> WriteDryvValidation(IEnumerable<KeyValuePair<string, Type>> validationSets, Func<Type, object> serviceProvider, IReadOnlyDictionary<string, object> parameters)
        {
            var resultSet = new Dictionary<string, (Dictionary<string, Action<TextWriter>> validators, Dictionary<string, Action<TextWriter>> disablers, Dictionary<string, object> parameters)>();

            foreach (var (setName, type) in validationSets)
            {
                var translation = await this.translator.TranslateValidationRules(type, serviceProvider, parameters);
                var ruleParameters = translation.Parameters;
                var validators = translation.ValidationFunctions.ToDictionary(i => i.Key, i => this.functionWriter.GetValidationFunction(i.Value));
                var disablers = translation.DisablingFunctions.ToDictionary(i => i.Key, i => this.functionWriter.GetValidationFunction(i.Value));

                resultSet.Add(setName, (validators, disablers, ruleParameters));
            }

            return writer =>
            {
                this.setWriter.WriteBegin(writer);

                foreach (var (setName, (validators, disablers, parameters)) in resultSet)
                {
                    this.setWriter.WriteValidationSet(writer, setName, validators, disablers, parameters);
                }

                this.setWriter.WriteEnd(writer);
            };
        }
    }
}