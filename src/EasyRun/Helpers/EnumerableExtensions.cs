using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyRun
{
    internal static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> @this, Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            foreach (var item in @this)
                action.Invoke(item);
        }

        public static IEnumerable<Type> EnumerateWithBaseTypes(this Type type, bool excludeSelf = false)
        {
            var currentType = excludeSelf ? type.BaseType : type;
            while (currentType != null && currentType != typeof(object))
            {
                yield return currentType;
                currentType = currentType.BaseType;
            }
        }

        public static IEnumerable<Type> EnumerateWithBaseTypesReverse(this Type type, bool excludeSelf = false)
        {
            return EnumerateWithBaseTypes(type, excludeSelf).Reverse();
        }
    }
}
