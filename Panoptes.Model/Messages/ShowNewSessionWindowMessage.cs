using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public class ShowNewSessionWindowMessage : ValueChangedMessage<string>
    {
        public ShowNewSessionWindowMessage() : base("New")
        { }
    }
}
