using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;

namespace Panoptes.Model.Messages
{
    public sealed class TradeFilterMessage : ValueChangedMessage<string>
    {
        public TradeFilterMessage(string source, DateTime? fromDate, DateTime? toDate) : base(source)
        {
            FromDate = fromDate;
            ToDate = toDate;
        }

        public string Source => base.Value;

        public DateTime? FromDate { get; }

        public DateTime? ToDate { get; }
    }
}
