using EasyRun.Builders;
using EasyRun.Resolvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyRun.Helpers
{
    internal static class EnumerableMaker
    {
        static readonly Dictionary<Type, Func<IEnumerable<IObjectBuilder>, ObjectResolver, IEnumerable>>
            enumerateFactoryCacheMap = new Dictionary<Type, Func<IEnumerable<IObjectBuilder>, ObjectResolver, IEnumerable>>();

        public static T MakeEnumerable<T>(
            Type elementType,
            IEnumerable<IObjectBuilder> objectBuilders,
            ObjectResolver resolver)
        {
            if (!enumerateFactoryCacheMap.TryGetValue(elementType, out var func))
                enumerateFactoryCacheMap.Add(elementType, CreateEnumerateFactory(elementType));
            return (T)func.Invoke(objectBuilders, resolver);
        }

        static Func<IEnumerable<IObjectBuilder>, ObjectResolver, IEnumerable> CreateEnumerateFactory(Type elementType)
        {
            return (Func<IEnumerable<IObjectBuilder>, ObjectResolver, IEnumerable>)
                typeof(Factory<>).MakeGenericType(elementType)
                    .GetMethod(nameof(MakeEnumerable), BindingFlags.Static | BindingFlags.Public)
                    .CreateDelegate(typeof(Func<IEnumerable<IObjectBuilder>, ObjectResolver, IEnumerable>));
        }

        static class Factory<T>
        {
            public static IEnumerable MakeEnumerable(
                IEnumerable<IObjectBuilder> objectBuilders,
                ObjectResolver resolver)
            {
                return objectBuilders
                    .OfType<IObjectBuilder<T>>()
                    .Select(builder => builder.Build(resolver));
            }
        }
    }


}
