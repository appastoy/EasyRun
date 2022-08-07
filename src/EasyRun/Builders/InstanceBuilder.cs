using EasyRun.Resolvers;
using System;

namespace EasyRun.Builders
{
    internal sealed class InstanceBuilder<T> : ObjectBuilder<T>
    {
        readonly T _instance;

        public InstanceBuilder(T instance) => _instance = instance;

        public override T Build(ObjectResolver _) => _instance;

        public override void Dispose()
        {
            if (_instance is IDisposable disposable)
                disposable.Dispose();
        }
    }
}