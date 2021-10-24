using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public class TradeSelectedMessage : ValueChangedMessage<int>
    {
        public string Sender { get; }
        public TradeSelectedMessage(string sender, int value) : base(value)
        {
            Sender = sender;
        }
    }
}
