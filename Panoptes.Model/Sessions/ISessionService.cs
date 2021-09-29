using System;

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

        void Open(ISessionParameters parameters);
    }
}
