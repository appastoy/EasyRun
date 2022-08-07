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
            Action<IDependencyRegistration> registerAction,
            Action<IRunnerRegistration> addRunnerAction)
            => CreateStaticRunner(registerAction, addRunnerAction).Run();

        public Task StaticRunAsync(
            Action<IDependencyRegistration> registerAction,
            Action<IRunnerRegistration> addRunnerAction,
            CancellationToken cancellationToken = default)
            => CreateStaticRunner(registerAction, addRunnerAction).RunAsync(cancellationToken);

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
            _lastRunnerTypeDescriptor.AddParam(name, factory);
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

        public void Run<TRunner>(Action<IObjectCreationOption> addParamAction = null)
            where TRunner : IRunner
            => CreateRunnerInstance<TRunner>(addParamAction).Run();

        public void Run(Action<IRunnerRegistration> addRunnerAction)
            => CreateCloneRunner(addRunnerAction).Run();

        public void Run(
            Action<IDependencyRegistration> subRegisterAction,
            Action<IRunnerRegistration> addRunnerAction)
            => CreateSubRunner(subRegisterAction, addRunnerAction).Run();

        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            var task = RunAsyncInteranl(cancellationToken);
            RunInternal();
            return task;
        }

        public Task RunAsync<TAsyncRunner>(
            Action<IObjectCreationOption> addParamAction = null,
            CancellationToken cancellationToken = default)
                where TAsyncRunner : IAsyncRunner
            => CreateRunnerInstance<TAsyncRunner>(addParamAction).RunAsync(cancellationToken);

        public Task RunAsync(Action<IRunnerRegistration> addRunnerAction, CancellationToken cancellationToken = default)
            => CreateCloneRunner(addRunnerAction).RunAsync(cancellationToken);

        public Task RunAsync(
            Action<IDependencyRegistration> subRegisterAction,
            Action<IRunnerRegistration> addRunnerAction,
            CancellationToken cancellationToken = default)
            => CreateSubRunner(subRegisterAction, addRunnerAction).RunAsync(cancellationToken);

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

        TRunner CreateRunnerInstance<TRunner>(Action<IObjectCreationOption> addParamAction)
        {
            var descriptor = new TypeDescriptor<TRunner, TRunner>(LifeTime.Transient);
            addParamAction?.Invoke(descriptor);
            return descriptor.CreateBuilderInternal().Build(_resolver);
        }

        private static Runner CreateStaticRunner(
            Action<IDependencyRegistration> registerAction,
            Action<IRunnerRegistration> addRunnerAction)
        {
            if (registerAction == null)
                throw new ArgumentNullException(nameof(registerAction));
            if (addRunnerAction == null)
                throw new ArgumentNullException(nameof(addRunnerAction));
            var builder = CreateBuilder();
            registerAction.Invoke(builder);
            var container = builder.Build();
            addRunnerAction.Invoke(container);
            return container;
        }

        private Runner CreateCloneRunner(Action<IRunnerRegistration> addRunnerAction)
        {
            if (addRunnerAction == null)
                throw new ArgumentNullException(nameof(addRunnerAction));
            var cloneContainer = Clone();
            addRunnerAction.Invoke(cloneContainer);
            return cloneContainer;
        }

        private Runner CreateSubRunner(
            Action<IDependencyRegistration> registerAction,
            Action<IRunnerRegistration> addRunnerAction)
        {
            if (addRunnerAction == null)
                throw new ArgumentNullException(nameof(addRunnerAction));

            var container = CreateSubRunner(registerAction);
            addRunnerAction.Invoke(container);
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