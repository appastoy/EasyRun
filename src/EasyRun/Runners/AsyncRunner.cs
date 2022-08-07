using EasyRun.Builders;
using EasyRun.Resolvers;
using System.Threading;
using System.Threading.Tasks;

namespace EasyRun.Runners
{
    internal sealed class AsyncRunner<TRunner> : IAsyncRunnerInternal
        where TRunner : IAsyncRunner
    {
        readonly ObjectBuilder<TRunner> _builder;

        public AsyncRunner(ObjectBuilder<TRunner> builder) 
            => _builder = builder;

        public Task RunAsync(ObjectResolver resolver, CancellationToken cancellationToken = default)
            => _builder.Build(resolver).RunAsync(cancellationToken);
    }
}
