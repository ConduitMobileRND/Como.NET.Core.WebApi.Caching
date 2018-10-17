using System;
using System.Linq;
using System.Reflection;

namespace Como.WebApi.Caching
{
    public static class ReflectionExtensionMethods
    {
        public static string GetUniqueIdentifier(this MethodInfo method)
        {
            var parameters = string.Join(", ",
                method.GetParameters().Select(p => p.ParameterType.GetUniqueIdentifier() + " " + p.Name));
            return $"{method.DeclaringType.GetUniqueIdentifier()}.{method.Name}({parameters})";
        }

        public static string GetUniqueIdentifier(this Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            var typeNameTagCharIndex = type.Name.IndexOf('`');
            var typeName = typeNameTagCharIndex == -1 ? type.Name : type.Name.Substring(0, typeNameTagCharIndex);
            var genericArguments = string.Join(", ", type.GetGenericArguments().Select(GetUniqueIdentifier));
            return $"{type.Namespace}.{typeName}<{genericArguments}>";
        }
    }
}