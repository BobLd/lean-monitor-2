﻿using Microsoft.Toolkit.Mvvm.Messaging.Messages;
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

            if (dateTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentOutOfRangeException(nameof(dateTime), $"LogEntryReceivedMessage: Should be provided with UTC date, received {dateTime.Kind}.");
            }

            DateTime = dateTime;
            Message = message;
            EntryType = entryType;
        }

        /// <summary>
        /// UTC time.
        /// </summary>
        public DateTime DateTime { get; }

        public string Message { get; }

        public LogItemType EntryType { get; }
    }
}
