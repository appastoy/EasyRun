using EasyRun.Resolvers;
using EasyRun.Runners;
using System;
using System.Threading.Tasks;

namespace EasyRun.Builders
{
    internal interface IObjectBuilder : IDisposable
    {
        
    }

    internal interface IObjectBuilder<T> : IObjectBuilder
    {
        T Build(ObjectResolver resolver);
    }

    internal abstract class ObjectBuilder<T> : IObjectBuilder<T>
    {
        public abstract T Build(ObjectResolver resolver);
        public abstract void Dispose();
    }
}