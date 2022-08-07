using EasyRun.Descriptors;
using EasyRun.Helpers;
using EasyRun.Resolvers;
using System.Collections.Generic;

namespace EasyRun.Builders
{
    internal sealed class TransientBuilder<T> : ObjectBuilder<T>
    {
        readonly FactoryDelegate<T> _factory;
        readonly IReadOnlyDictionary<string, IParameterDescriptor> _paramDescMap;

        public TransientBuilder(
            FactoryDelegate<T> factory
            , IReadOnlyDictionary<string, IParameterDescriptor> paramDescMap)
        {
            _factory = factory;
            _paramDescMap = paramDescMap;
        }

        public override T Build(ObjectResolver resolver) => _factory.Invoke(resolver, _paramDescMap);

        public override void Dispose() { }
    }
}