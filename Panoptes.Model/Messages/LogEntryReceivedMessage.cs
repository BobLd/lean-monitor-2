using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;

namespace Panoptes.Model.Messages
{
    public sealed class LogEntryReceivedMessage : ValueChangedMessage<object>
    {
        public LogEntryReceivedMessage(object obj) : base(obj)
        { }

        public LogEntryReceivedMessage(DateTime dateTime, string message, LogItemType entryType)
            : this((dateTime, message, entryType))
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            DateTime = dateTime;
            Message = message;
            EntryType = entryType;
        }

        public DateTime DateTime { get; private set; }
        public string Message { get; private set; }
        public LogItemType EntryType { get; private set; }
    }
}
