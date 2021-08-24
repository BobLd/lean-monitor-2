using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Panoptes.Model.Sessions;

namespace Panoptes.Model.Messages
{
    public class SessionStateChangedMessage : ValueChangedMessage<SessionState>
    {
        public SessionStateChangedMessage(SessionState state) : base(state)
        { }

        public SessionState State => Value;
    }
}
