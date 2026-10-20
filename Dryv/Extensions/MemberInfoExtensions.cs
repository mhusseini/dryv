using System;
using System.Reflection;

namespace Dryv.Extensions
{
    internal static class MemberInfoExtensions
    {
        public static Type GetMemberType(this MemberInfo member)
        {
            return member switch
            {
                PropertyInfo pi => pi.PropertyType,
                FieldInfo fi => fi.FieldType,
                MethodInfo mi => mi.ReturnType,
                _ => throw new NotSupportedException($"Unsupported member type: {member.GetType()}")
            };
        }
    }
}