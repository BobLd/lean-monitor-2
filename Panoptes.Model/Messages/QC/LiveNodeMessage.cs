using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Panoptes.Model.Serialization.Packets;

namespace Panoptes.Model.Messages
{
    public sealed class LiveNodeMessage : ValueChangedMessage<LiveNodePacket>
    {
        public LiveNodeMessage(LiveNodePacket value)
            : base(value)
        { }
    }
}
