using EasyRun.Builders;
using EasyRun.Registrations;
using System;

namespace EasyRun.Descriptors
{
    internal sealed class InstanceDescriptor<T> : IObjectDescriptor
    {
        public Type Type { get; }
        public LifeTime LifeTime => LifeTime.Scoped;
        public T Instance { get; }

        public InstanceDescriptor(Type type, T instance)
        {
            Type = type;
            Instance = instance;
        }

        public IObjectBuilder CreateBuilder() => new InstanceBuilder<T>(Instance);
    }
}
