using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public sealed class SessionClosedMessage : ValueChangedMessage<string>
    {
        public SessionClosedMessage() : base("Close")
        {

        }
    }
}
