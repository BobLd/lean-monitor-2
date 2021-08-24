using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public class SessionClosedMessage : ValueChangedMessage<string>
    {
        public SessionClosedMessage() : base("Close")
        {

        }
    }
}
