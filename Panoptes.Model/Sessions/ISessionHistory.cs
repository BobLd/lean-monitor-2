using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Model.Sessions
{
    public interface ISessionHistory
    {
        Task LoadRecentDataAsync(CancellationToken cancellationToken);
    }
}
