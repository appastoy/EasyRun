using EasyRun.Builders;
using EasyRun.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyRun.Resolvers
{

    sealed class ObjectResolver : IObjectResolver
    {
        readonly IReadOnlyDictionary<Type, IReadOnlyList<IObjectBuilder>> _objectBuilderMap;
        readonly ObjectResolver _parentResolver;

        internal ObjectResolver(
            IReadOnlyDictionary<Type, IReadOnlyList<IObjectBuilder>> objectBuilderMap,
            ObjectResolver parentResolver)
        {
            _objectBuilderMap = objectBuilderMap;
            _parentResolver = parentResolver;
        }

        internal ObjectResolver Prebuild()
        {
            var scopedBuilders = _objectBuilderMap.Values
                                                 .SelectMany(builders => builders)
                                                 .OfType<IScopedBuilder>();
            foreach (var scopedBuilder in scopedBuilders)
                scopedBuilder.Prebuild(this);
            return this;
        }

        public T Resolve<T>()
        {
            return TryResolve<T>(out var value) ? value :
                throw new InvalidOperationException($"Can't resolve type. (type: {typeof(T).Name})");
        }

        public bool TryResolve<T>(out T value)
        {
            return TryResolveInternal(out value) ||
                TryResolveAsEnumerable(out value);
        }

        public void Inject<T>(T value)
        {
            InjectDelegateCache.Acquire<T>().Invoke(this, value);
        }

        bool TryResolveInternal<T>(out T value)
        {
            var builder = FindObjectBuilder<T>();
            if (builder != null)
            {
                value = builder.Build(this);
                return true;
            }
            value = default;
            return false;
        }

        IObjectBuilder<T> FindObjectBuilder<T>()
        {
            var currentResolver = this;
            while (currentResolver != null)
            {
                if (currentResolver._objectBuilderMap.TryGetValue(typeof(T), out var list) &&
                    list[0] is IObjectBuilder<T> builder)
                    return builder;
                currentResolver = currentResolver._parentResolver;
            }
            return null;
        }

        bool TryResolveAsEnumerable<T>(out T value)
        {
            value = default;
            var type = typeof(T);
            if (!type.IsInterface || !type.IsGenericType)
                return false;

            var definition = type.GetGenericTypeDefinition();
            if (definition != typeof(IEnumerable<>))
                return false;

            var elementType = type.GetGenericArguments()[0];
            var objectBuilders = CollectObjectBuilders(elementType);
            if (!objectBuilders.Any())
                return false;

            value = EnumerableMaker.MakeEnumerable<T>(
                elementType,
                objectBuilders.SelectMany(list => list),
                this);
            return true;
        }

        IEnumerable<IEnumerable<IObjectBuilder>> CollectObjectBuilders(Type type)
        {
            return (_parentResolver?.CollectObjectBuilders(type) ??
                    Enumerable.Empty<IEnumerable<IObjectBuilder>>())
                .Concat(Enumerable.Repeat(GetObjectBuilders(type), 1));
        }

        IEnumerable<IObjectBuilder> GetObjectBuilders(Type type)
        {
            return _objectBuilderMap.TryGetValue(type, out var list) ?
                list : Enumerable.Empty<IObjectBuilder>();
        }

        internal void Dispose()
        {
            foreach (var item in _objectBuilderMap.Values.SelectMany(list => list))
                item.Dispose();
        }
    }
}
