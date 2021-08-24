﻿using QuantConnect.Packets;

namespace Panoptes.Model.Sessions
{
    public interface ISessionHandler
    {
        void HandleStateChanged(SessionState state);

        void HandleResult(ResultContext resultContext);

        void HandleLogMessage(string message, LogItemType type);

        void HandleOrderEvent(OrderEventPacket orderEvent);

        void HandleAlgorithmStatus(AlgorithmStatusPacket algorithmStatusPacket);
    }
}
