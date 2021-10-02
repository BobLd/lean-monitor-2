using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;

namespace Panoptes.Model.Messages
{
    public class FilterMessage : ValueChangedMessage<string>
    {
        public FilterMessage(string source, DateTime? fromDate, DateTime? toDate) : base(source)
        {
            FromDate = fromDate;
            ToDate = toDate;
        }

        public string Source => base.Value;

        public DateTime? FromDate { get; }

        public DateTime? ToDate { get; }
    }
}
