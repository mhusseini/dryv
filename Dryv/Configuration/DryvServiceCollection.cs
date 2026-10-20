using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dryv.Configuration
{
    public class DryvServiceCollection : IEnumerable<Type>
    {
        private readonly List<Type> items = new();
        private readonly Type[] requiredInterfaces;

        public DryvServiceCollection(params Type[] requiredInterfaces)
        {
            this.requiredInterfaces = requiredInterfaces ?? Array.Empty<Type>();
        }

        protected virtual void OnAdd(Type type)
        {

        }
        protected virtual void OnRemove(Type type)
        {

        }
        protected virtual void OnClear()
        {

        }

        public void Add(Type type)
        {
            if (this.requiredInterfaces.Length > 0 && !this.requiredInterfaces.Any(i => i.IsAssignableFrom(type)))
            {
                throw new ArgumentException(
                    $"Type '{type.FullName}' must implement one of: {string.Join(", ", this.requiredInterfaces.Select(i => i.Name))}.",
                    nameof(type));
            }

            this.OnAdd(type);
            this.items.Add(type);
        }

        public void Add<T>() => this.Add(typeof(T));

        public void Clear()
        {
            this.OnClear();
            this.items.Clear();
        }

        public IEnumerator<Type> GetEnumerator() => this.items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.items).GetEnumerator();

        public void Remove(Type type)
        {
            this.OnRemove(type);
            this.items.Remove(type);
        }

        public void Remove<T>() => this.Remove(typeof(T));
    }
}