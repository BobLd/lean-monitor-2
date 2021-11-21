using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Model.Sessions
{
    public interface ISessionService
    {
        bool IsSessionActive { get; }

        Result LastResult { get; }

        void Initialize();

        void ShutdownSession();

        bool IsSessionSubscribed { get; set; }

        bool CanSubscribe { get; }

        /// <summary>
        /// Open the session async.
        /// </summary>
        /// <param name="parameters"></param>
        Task OpenAsync(ISessionParameters parameters, CancellationToken cancellationToken);
    }
}
