using EasyRun.Builders;
using EasyRun.Resolvers;

namespace EasyRun.Runners
{
    internal sealed class Runner<TRunner> : IRunnerInternal
        where TRunner : IRunner
    {
        readonly ObjectBuilder<TRunner> _builder;

        public Runner(ObjectBuilder<TRunner> builder)
            => _builder = builder;

        public void Run(ObjectResolver resolver)
            => _builder.Build(resolver).Run();
    }
}
