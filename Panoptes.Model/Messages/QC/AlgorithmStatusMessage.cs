using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Panoptes.Model.Serialization.Packets;

namespace Panoptes.Model.Messages
{
    public sealed class AlgorithmStatusMessage : ValueChangedMessage<AlgorithmStatusPacket>
    {
        public AlgorithmStatusMessage(AlgorithmStatusPacket algorithmStatus)
            : base(algorithmStatus)
        { }
    }
}
