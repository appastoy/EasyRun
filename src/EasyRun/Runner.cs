using EasyRun.Registrations;
using EasyRun.Descriptors;
using EasyRun.Resolvers;
using EasyRun.Runners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyRun
{
    public sealed class Runner : IRunnerRegistration, IDisposable
    {
        public static RunnerBuilder CreateBuilder() => new RunnerBuilder(null);

        public static void StaticRun(
            Action<IDependencyRegistration> builderAction,
            Action<IRunnerRegistration> runnerAction)
            => CreateStaticRunner(builderAction, runnerAction).Run();

        public static Task StaticRunAsync(
            Action<IDependencyRegistration> builderAction,
            Action<IRunnerRegistration> runnerAction,
            CancellationToken cancellationToken = default)
            => CreateStaticRunner(builderAction, runnerAction).RunAsync(cancellationToken);

        readonly ObjectResolver _resolver;
        readonly List<RunnerInfo> _runnerInfos = new List<RunnerInfo>();
        readonly List<RunnerInfo> _asyncRunnerInfos = new List<RunnerInfo>();
        ITypeDescriptor _lastRunnerTypeDescriptor;

        public IObjectResolver Resolver => _resolver;

        internal Runner(ObjectResolver resolver) => _resolver = resolver;

        public Runner Clone() => new Runner(_resolver);

        public RunnerBuilder CreateSubBuilder() => new RunnerBuilder(_resolver);

        public Runner CreateSubRunner(Action<RunnerBuilder> registerAction)
        {
            if (registerAction == null)
                throw new ArgumentNullException(nameof(registerAction));

            var builder = CreateSubBuilder();
            registerAction.Invoke(builder);
            return builder.Build();
        }

        public Runner Add<TRunner>(int priority = 0) where TRunner : IRunner
            => AddRunnerInternal<TRunner>(_runnerInfos, priority, false);
        public Runner AddAsync<TAsyncRunner>(int priority = 0) where TAsyncRunner : IAsyncRunner
            => AddRunnerInternal<TAsyncRunner>(_asyncRunnerInfos, priority, true);
        public Runner TryAdd<TRunner>(out bool isAdded, int priority = 0) where TRunner : IRunner
            => (isAdded = !IsRunnerAdded(typeof(TRunner), _runnerInfos)) ?
                Add<TRunner>(priority) : this;
        public Runner TryAddAsync<TAsyncRunner>(out bool isAdded, int priority = 0) where TAsyncRunner : IAsyncRunner
            => (isAdded = !IsRunnerAdded(typeof(TAsyncRunner), _asyncRunnerInfos)) ?
                AddAsync<TAsyncRunner>(priority) : this;

        public Runner WithParam<T>(string name, T value)
        {
            if (_lastRunnerTypeDescriptor == null)
                throw new InvalidOperationException("You should call Add() or AddAsync() method before call WithParam() method.");
            _lastRunnerTypeDescriptor.AddParam(name, value);
            return this;
        }

        public Runner WithParamFactory<T>(string name, Func<T> factory)
        {
            if (_lastRunnerTypeDescriptor == null)
                throw new InvalidOperationException("You should call Add() or AddAsync() method before call WithParamFactory() method.");
            _lastRunnerTypeDescriptor.AddParamFactory(name, factory);
            return this;
        }

        public void Run()
        {
            var task = RunAsync();
            while (!task.IsCompleted)
                Thread.Yield();
            if (task.IsFaulted)
                throw task.Exception;
        }

        public void RunDirect<TRunner>(Action<IObjectCreationOption> option = null)
            where TRunner : IRunner
            => CreateRunnerInstance<TRunner>(option).Run();

        public void RunTemp(Action<IRunnerRegistration> runnerAction)
            => CreateCloneRunner(runnerAction).Run();

        public void RunSub(
            Action<IDependencyRegistration> builderAction,
            Action<IRunnerRegistration> runnerAction)
            => CreateSubRunner(builderAction, runnerAction).Run();

        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            var task = RunAsyncInteranl(cancellationToken);
            RunInternal();
            return task;
        }

        public Task RunDirectAsync<TAsyncRunner>(
            Action<IObjectCreationOption> option = null,
            CancellationToken cancellationToken = default)
                where TAsyncRunner : IAsyncRunner
            => CreateRunnerInstance<TAsyncRunner>(option).RunAsync(cancellationToken);

        public Task RunTempAsync(
            Action<IRunnerRegistration> runnerAction, 
            CancellationToken cancellationToken = default)
            => CreateCloneRunner(runnerAction).RunAsync(cancellationToken);

        public Task RunSubAsync(
            Action<IDependencyRegistration> builderAction,
            Action<IRunnerRegistration> runnerAction,
            CancellationToken cancellationToken = default)
            => CreateSubRunner(builderAction, runnerAction).RunAsync(cancellationToken);

        public void Dispose() => _resolver.Dispose();

        void RunInternal()
        {
            _runnerInfos.OrderBy(i => i.Priority)
                        .Select(i => i.CreateRunner())
                        .ForEach(runner => runner.Run(_resolver));
        }

        Task RunAsyncInteranl(CancellationToken cancellationToken)
        {
            return Task.WhenAll(_asyncRunnerInfos
                .OrderBy(i => i.Priority)
                .Select(item => item.CreateAsyncRunner().RunAsync(_resolver, cancellationToken)));
        }

        bool IsRunnerAdded(Type type, List<RunnerInfo> list)
            => list.Any(item => item.Type == type);

        Runner AddRunnerInternal<TRunner>(List<RunnerInfo> list, int priority, bool isAsync)
        {
            if (IsRunnerAdded(typeof(TRunner), list))
                throw new ArgumentException($"{typeof(TRunner).Name} type is already added as {(isAsync ? "async " : "")}runner.");
            list.Add(new RunnerInfo(
                _lastRunnerTypeDescriptor = new TypeDescriptor<TRunner, TRunner>(LifeTime.Transient),
                priority));
            return this;
        }

        TRunner CreateRunnerInstance<TRunner>(Action<IObjectCreationOption> option)
        {
            var descriptor = new TypeDescriptor<TRunner, TRunner>(LifeTime.Transient);
            option?.Invoke(descriptor);
            return descriptor.CreateBuilderInternal().Build(_resolver);
        }

        private static Runner CreateStaticRunner(
            Action<IDependencyRegistration> builderAction,
            Action<IRunnerRegistration> runnerAction)
        {
            if (builderAction == null)
                throw new ArgumentNullException(nameof(builderAction));
            if (runnerAction == null)
                throw new ArgumentNullException(nameof(runnerAction));
            var builder = CreateBuilder();
            builderAction.Invoke(builder);
            var container = builder.Build();
            runnerAction.Invoke(container);
            return container;
        }

        private Runner CreateCloneRunner(Action<IRunnerRegistration> runnerAction)
        {
            if (runnerAction == null)
                throw new ArgumentNullException(nameof(runnerAction));
            var cloneContainer = Clone();
            runnerAction.Invoke(cloneContainer);
            return cloneContainer;
        }

        private Runner CreateSubRunner(
            Action<IDependencyRegistration> builderAction,
            Action<IRunnerRegistration> runnerAction)
        {
            if (runnerAction == null)
                throw new ArgumentNullException(nameof(runnerAction));

            var container = CreateSubRunner(builderAction);
            runnerAction.Invoke(container);
            return container;
        }

        IRunnerRegistration IRunnerRegistration.Add<TRunner>(int priority)
            => Add<TRunner>(priority);

        IRunnerRegistration IRunnerRegistration.AddAsync<TAsyncRunner>(int priority)
            => AddAsync<TAsyncRunner>(priority);

        IRunnerRegistration IRunnerRegistration.TryAdd<TRunner>(out bool isAdded, int priority)
            => TryAdd<TRunner>(out isAdded, priority);

        IRunnerRegistration IRunnerRegistration.TryAddAsync<TAsyncRunner>(out bool isAdded, int priority)
            => TryAddAsync<TAsyncRunner>(out isAdded, priority);

        IRunnerRegistration IRunnerRegistration.WithParam<T>(string name, T value)
            => WithParam(name, value);

        IRunnerRegistration IRunnerRegistration.WithParamFactory<T>(string name, Func<T> factory)
            => WithParamFactory(name, factory);
    }
}