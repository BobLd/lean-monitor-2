using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public class TimerMessage : ValueChangedMessage<object>
    {
        public TimerMessage(object value) : base(value)
        {
        }
    }
}
