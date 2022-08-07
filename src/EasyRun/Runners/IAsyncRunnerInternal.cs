using EasyRun.Resolvers;
using System.Threading;
using System.Threading.Tasks;

namespace EasyRun.Runners
{
    internal interface IAsyncRunnerInternal
    {
        Task RunAsync(ObjectResolver resolver, CancellationToken cancellationToken = default);
    }
}
