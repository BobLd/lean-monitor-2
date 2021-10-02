using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;

namespace Panoptes.Model.Messages
{
    public sealed class SessionUpdateMessage : ValueChangedMessage<ResultContext>
    {
        public SessionUpdateMessage(ResultContext resultContext)
            : base(resultContext ?? throw new ArgumentNullException(nameof(resultContext)))
        {
        }

        public ResultContext ResultContext => Value;
    }
}
