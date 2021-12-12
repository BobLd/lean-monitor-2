using Panoptes.Model.Serialization.Packets;
using System;

namespace Panoptes.Model.Sessions
{
    public interface ISessionHandler
    {
        void HandleStateChanged(SessionState state);

        void HandleResult(ResultContext resultContext);

        void HandleLogMessage(DateTime utcTime, string message, LogItemType type);

        void HandleOrderEvent(OrderEventPacket orderEvent);

        void HandleAlgorithmStatus(AlgorithmStatusPacket algorithmStatusPacket);

        void HandleLiveNode(LiveNodePacket algorithmStatusPacket);
    }
}
