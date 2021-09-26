using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using QuantConnect.Packets;

namespace Panoptes.Model.Messages
{
    public sealed class AlgorithmStatusMessage : ValueChangedMessage<AlgorithmStatusPacket>
    {
        public AlgorithmStatusMessage(AlgorithmStatusPacket algorithmStatus)
            : base(algorithmStatus)
        { }
    }
}
