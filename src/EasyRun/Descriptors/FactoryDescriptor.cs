using EasyRun.Builders;
using EasyRun.Helpers;
using EasyRun.Resolvers;
using System;

namespace EasyRun.Descriptors
{
    internal sealed class FactoryDescriptor<T> : IObjectDescriptor
    {
        private readonly FactoryDelegate<T> _factory;

        public Type Type { get; }
        public LifeTime LifeTime { get; }

        public FactoryDescriptor(Type type, LifeTime lifeTime, Func<ObjectResolver, T> factory)
        {
            Type = type;
            LifeTime = lifeTime;
            _factory = (resolver, _) => factory.Invoke(resolver);
        }

        public IObjectBuilder CreateBuilder()
            => LifeTime == LifeTime.Transient ?
                (IObjectBuilder)new TransientBuilder<T>(_factory, null) :
                new ScopedBuilder<T>(_factory, null);
    }
}
