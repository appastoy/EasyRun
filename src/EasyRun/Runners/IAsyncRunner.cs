using System.Threading;
using System.Threading.Tasks;

namespace EasyRun.Runners
{
    public interface IAsyncRunner
    {
        Task RunAsync(CancellationToken cancellationToken = default);
    }
}
