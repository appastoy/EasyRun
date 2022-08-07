using EasyRun.Runners;
using System;

namespace EasyRun.Registrations
{
    public interface IRunnerRegistration
    {
        IRunnerRegistration Add<TRunner>(int priority = 0) where TRunner : IRunner;
        IRunnerRegistration AddAsync<TAsyncRunner>(int priority = 0) where TAsyncRunner : IAsyncRunner;
        IRunnerRegistration TryAdd<TRunner>(out bool isAdded, int priority = 0) where TRunner : IRunner;
        IRunnerRegistration TryAddAsync<TAsyncRunner>(out bool isAdded, int priority = 0) where TAsyncRunner : IAsyncRunner;
        IRunnerRegistration WithParam<T>(string name, T value);
        IRunnerRegistration WithParamFactory<T>(string name, Func<T> factory);
    }
}
