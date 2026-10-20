using System;
using Dryv.Configuration;

namespace Dryv.AspNetCore.Internal
{
    internal class RegisteringDryvServiceCollection(Action<Type> typeAdded, params Type[] requiredInterfaces) : DryvServiceCollection(requiredInterfaces)
    {
        protected override void OnAdd(Type type)
        {
            typeAdded(type);
        }
    }
}