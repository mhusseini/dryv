using System;
using System.Linq;
using System.Reflection;

namespace Dryv.Extensions
{
    internal static class TypeExtensions
    {
        public static Type GetElementTypeOrSelf(this Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType() ?? type;
            }

            if (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    return genericArgs[0];
                }
            }

            return type;
        }

        public static string GetNonGenericName(this Type type)
        {
            return type.FullName;
            //return type.GenericTypeArguments.Length == 0 ? type.FullName : type.GetGenericArguments().First().FullName;
        }

        public static bool IsEnumOrNullableEnum(this Type type, out Type enumType)
        {
            enumType = null;
            
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsEnum)
            {
                enumType = type;
                return true;
            }

            if (!typeInfo.IsGenericType || typeInfo.GetGenericTypeDefinition() != typeof(Nullable<>))
            {
                return false;
            }
            
            var innerType = typeInfo.GenericTypeArguments.First();
            if (!innerType.GetTypeInfo().IsEnum)
            {
                return false;
            }
            
            enumType = innerType;
            return true;

        }
    }
}