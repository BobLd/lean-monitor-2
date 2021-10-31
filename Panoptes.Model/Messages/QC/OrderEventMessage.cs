using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Panoptes.Model.Serialization.Packets;
using System;

namespace Panoptes.Model.Messages
{
    public sealed class OrderEventMessage : ValueChangedMessage<OrderEventPacket>
    {
        public OrderEventMessage(OrderEventPacket orderEvent)
            : base(orderEvent ?? throw new ArgumentNullException(nameof(orderEvent)))
        {
        }
    }
}
