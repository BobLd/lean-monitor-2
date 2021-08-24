using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using QuantConnect.Packets;
using System;

namespace Panoptes.Model.Messages
{
    public class OrderEventMessage : ValueChangedMessage<OrderEventPacket>
    {
        public OrderEventMessage(OrderEventPacket orderEvent)
            : base(orderEvent ?? throw new ArgumentNullException(nameof(orderEvent)))
        {
        }
    }
}
