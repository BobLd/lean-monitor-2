using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public sealed class SessionOpenedMessage : ValueChangedMessage<string>
    {
        public SessionOpenedMessage() : base("Open")
        {

        }
    }
}
