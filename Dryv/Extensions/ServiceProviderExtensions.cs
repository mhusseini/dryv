using System;
using System.Collections.Generic;
using Dryv.Rules;

namespace Dryv.Extensions
{
    internal static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IDryvServiceResolver serviceResolver) where T : class
        {
            var o = serviceResolver.GetService(typeof(T));
            return o as T;
        }
        
        public static object[] GetServices(this IDryvServiceResolver serviceResolver, DryvCompiledRule rule, IReadOnlyDictionary<IReadOnlyList<DryvCompiledRule>, DryvParameters> parameters)
        {
            parameters.TryGetValue(rule.Parameters, out var dryvParameters);
            return GetServices(serviceResolver, rule.ServiceTypes, dryvParameters);
        }

        public static object[] GetServices(this IDryvServiceResolver serviceResolver, IList<Type> types, DryvParameters parameters = null)
        {
            var result = new object[types.Count];

            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var service = type == typeof(DryvParameters)
                ? parameters
                : serviceResolver.GetService(type);

                result[i] = service ?? throw new DryvDependencyInjectionException(type);
            }

            return result;
        }
    }
}