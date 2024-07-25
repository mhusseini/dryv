using System;
using System.Collections.Generic;
using System.IO;
using Dryv.Configuration;

namespace Dryv.Validation
{
    public class DryvWindowValidationSetWriter : DryvClientValidationSetWriter
    {
        public DryvWindowValidationSetWriter(DryvOptions options) : base(options)
        {
        }

        public override void WriteBegin(TextWriter writer)
        {
            writer.WriteLine("(function(dryv) { if (!dryv.v) { dryv.v = {}; }");
        }

        public override void WriteEnd(TextWriter writer)
        {
            writer.Write("})(window.dryv || (window.dryv = {}));");
        }

        public override void WriteValidationSet(TextWriter writer, string validationSetName, IDictionary<string, Action<TextWriter>> validators, IDictionary<string, Action<TextWriter>> disablers, IDictionary<string, object> parameters)
        {
            writer.Write(@"dryv.v[""");
            writer.Write(validationSetName);
            writer.Write(@"""] =");

            base.WriteValidationSet(writer, validationSetName, validators, disablers, parameters);
        }
    }
}