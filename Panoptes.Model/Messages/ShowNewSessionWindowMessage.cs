using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public sealed class ShowNewSessionWindowMessage : ValueChangedMessage<string>
    {
        public ShowNewSessionWindowMessage() : base("New")
        { }
    }
}
