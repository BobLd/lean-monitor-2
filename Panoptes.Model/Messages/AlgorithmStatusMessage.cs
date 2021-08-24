using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using QuantConnect.Packets;

namespace Panoptes.Model.Messages
{
    public class AlgorithmStatusMessage : ValueChangedMessage<AlgorithmStatusPacket>
    {
        public AlgorithmStatusMessage(AlgorithmStatusPacket algorithmStatus)
            : base(algorithmStatus)
        { }
    }
}
