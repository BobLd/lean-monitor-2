using System;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Model.Sessions
{
    public interface ISession : IDisposable
    {
        void Initialize();

        Task InitializeAsync(CancellationToken cancellationToken);

        void Shutdown();

        void Subscribe();

        void Unsubscribe();

        bool CanSubscribe { get; }

        SessionState State { get; }
    }
}
