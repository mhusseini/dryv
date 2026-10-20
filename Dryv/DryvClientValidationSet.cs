using System.Collections.Generic;

namespace Dryv
{
    public sealed class DryvClientValidationSet
    {
        public string Name { get; set; }

        public List<Touple2<string, object>> Parameters { get; set; } = new();

        public List<Touple2<string, string>> Validators { get; set; } = new();

        public List<Touple2<string, string>> Disablers { get; set; } = new();
    }

    public sealed class Touple2<T1, T2>
    {
        public Touple2()
        {
        }

        public Touple2(T1 key, T2 value)
        {
            this.Key = key;
            this.Value = value;
        }

        public T2 Value { get; set; }

        public T1 Key { get; set; }
    }
}