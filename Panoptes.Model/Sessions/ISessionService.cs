using System;
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
        /// Need to make Async
        /// </summary>
        /// <param name="parameters"></param>
        Task Open(ISessionParameters parameters, CancellationToken cancellationToken);
    }
}
