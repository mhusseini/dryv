using System;
using System.Collections.Generic;
using System.IO;
using Dryv.Configuration;
using Dryv.Extensions;
using Dryv.Translation;

namespace Dryv.Validation
{
    public class DryvClientValidationSetWriter : IDryvClientValidationSetWriter
    {
        private readonly DryvOptions options;

        public DryvClientValidationSetWriter(DryvOptions options)
        {
            this.options = options;
        }

        public virtual void WriteBegin(TextWriter writer)
        {
        }

        public virtual void WriteEnd(TextWriter writer)
        {
        }

        public virtual void WriteValidationSet(TextWriter writer, string validationSetName, IDictionary<string, Action<TextWriter>> validators, IDictionary<string, Action<TextWriter>> disablers, IDictionary<string, object> parameters)
        {
            writer.Write("{ name: ");
            writer.Write(JavaScriptHelper.TranslateValue(validationSetName));
            writer.Write(", validators: ");
            WriteObject(writer, validators);
            writer.Write(", disablers: ");
            WriteObject(writer, disablers);
            writer.Write(", parameters: ");
            this.WriteParameters(writer, parameters);
            writer.Write("}");
        }

        private void WriteParameters(TextWriter writer, IDictionary<string, object> parameters)
        {
            var sep = string.Empty;
            writer.Write("{");

            foreach (var parameter in parameters)
            {
                writer.Write(sep);
                writer.Write("\"");
                writer.Write(parameter.Key.ToCamelCase());
                writer.Write("\":");
                var jsValue = JavaScriptHelper.TranslateValue(parameter.Value) ?? this.options.JsonConversion(parameter.Value);
                writer.Write(jsValue);
                sep = ",";
            }

            writer.Write("}");
        }

        private static void WriteObject(TextWriter writer, IDictionary<string, Action<TextWriter>> items)
        {
            var sep = string.Empty;

            writer.Write("{");

            foreach (var item in items)
            {
                writer.Write(sep);
                writer.Write(@"""");
                writer.Write(item.Key);
                writer.Write(@""":");
                item.Value(writer);
                sep = ",";
            }

            writer.Write("}");
        }
    }
}