using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public class SessionOpenedMessage : ValueChangedMessage<string>
    {
        public SessionOpenedMessage() : base("Open")
        {

        }
    }
}
