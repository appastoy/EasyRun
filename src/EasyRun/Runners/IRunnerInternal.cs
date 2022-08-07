using EasyRun.Resolvers;

namespace EasyRun.Runners
{
    internal interface IRunnerInternal
    {
        void Run(ObjectResolver resolver);
    }
}
