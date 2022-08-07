using EasyRun.Builders;
using EasyRun.Descriptors;
using System;

namespace EasyRun.Runners
{
    internal readonly struct RunnerInfo
    {
        readonly ITypeDescriptor _descriptor;
        public Type Type => _descriptor.Type;
        public readonly int Priority;
        public RunnerInfo(ITypeDescriptor descriptor, int priority)
        {
            _descriptor = descriptor;
            Priority = priority;
        }

        public IRunnerInternal CreateRunner()
        {
            return CreateRunnerInternal<IRunnerInternal>(typeof(RunnerFactory<>), Type, _descriptor);
        } 

        public IAsyncRunnerInternal CreateAsyncRunner()
        {
            return CreateRunnerInternal<IAsyncRunnerInternal>(typeof(AsyncRunnerFactory<>), Type, _descriptor);
        }

        static TRunner CreateRunnerInternal<TRunner>(
            Type factoryType, 
            Type runnerType, 
            ITypeDescriptor descriptor)
        {
            return ((Func<ITypeDescriptor, TRunner>)
                factoryType
                    .MakeGenericType(runnerType)
                    .GetMethod("Create")
                    .CreateDelegate(typeof(Func<ITypeDescriptor, TRunner>)))
                .Invoke(descriptor);
        }

        static class RunnerFactory<T> where T : IRunner
        {
            public static IRunnerInternal Create(ITypeDescriptor typeDescriptor)
                => new Runner<T>(typeDescriptor.CreateBuilder() as ObjectBuilder<T>);
        }

        static class AsyncRunnerFactory<T> where T : IAsyncRunner
        {
            public static IAsyncRunnerInternal Create(ITypeDescriptor typeDescriptor)
                => new AsyncRunner<T>(typeDescriptor.CreateBuilder() as ObjectBuilder<T>);
        }
    }
}
