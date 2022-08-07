using EasyRun.Descriptors;
using EasyRun.Helpers;
using EasyRun.Resolvers;
using System;
using System.Collections.Generic;

namespace EasyRun.Builders
{
    internal interface IScopedBuilder
    {
        void Prebuild(ObjectResolver resolver);
    }

    internal sealed class ScopedBuilder<T> : ObjectBuilder<T>, IScopedBuilder
    {
        readonly FactoryDelegate<T> _factory;
        readonly IReadOnlyDictionary<string, IParameterDescriptor> _paramDescMap;
        bool _isCreated;
        T _instance;

        public ScopedBuilder(
            FactoryDelegate<T> factory
            , IReadOnlyDictionary<string, IParameterDescriptor> paramDescMap)
        {
            _factory = factory;
            _paramDescMap = paramDescMap;
        }

        public override T Build(ObjectResolver resolver)
        {
            if (!_isCreated)
            {
                _instance = _factory.Invoke(resolver, _paramDescMap);
                _isCreated = true;
            }
            return _instance;
        }

        public override void Dispose()
        {
            if (_instance is IDisposable disposable)
                disposable.Dispose();
        }

        void IScopedBuilder.Prebuild(ObjectResolver resolver) => Build(resolver);
    }
}