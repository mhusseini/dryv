using System.Collections.Generic;

namespace Dryv
{
    public sealed class DryvClientValidationSet
    {
        public string Name { get; set; }

        public List<KeyValuePair<string, object>> Parameters { get; set; } = new();

        public List<KeyValuePair<string, string>> Validators { get; set; } = new();

        public List<KeyValuePair<string, string>> Disablers { get; set; } = new();
    }
}