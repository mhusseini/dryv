using System;

namespace Dryv
{
    /// <summary>
    /// Abstracts service resolution so that the core library stays independent
    /// of any particular DI container while still supporting <see cref="IServiceProvider"/>
    /// features (scoping, better error messages) in framework-specific layers.
    /// </summary>
    public interface IDryvServiceResolver
    {
        object GetService(Type serviceType);
    }

    /// <summary>
    /// Wraps a <see cref="Func{Type, Object}"/> delegate as an <see cref="IDryvServiceResolver"/>.
    /// </summary>
    internal sealed class FuncServiceResolver : IDryvServiceResolver
    {
        private readonly Func<Type, object> resolver;

        public FuncServiceResolver(Func<Type, object> resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public object GetService(Type serviceType) => this.resolver(serviceType);
    }

    public static class DryvServiceResolverFactory
    {
        /// <summary>
        /// Creates an <see cref="IDryvServiceResolver"/> from a delegate.
        /// </summary>
        public static IDryvServiceResolver FromFunc(Func<Type, object> resolver) => new FuncServiceResolver(resolver);
    }
}
