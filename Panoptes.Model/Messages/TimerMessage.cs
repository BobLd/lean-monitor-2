using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using static Panoptes.Model.Messages.TimerMessage;

namespace Panoptes.Model.Messages
{
    /// <summary>
    /// In UTC time.
    /// </summary>
    public sealed class TimerMessage : ValueChangedMessage<TimerEventType>
    {
        public enum TimerEventType
        {
            NewMinute = 0,
            NewHour = 1,
            NewDay = 2,
            NewWeek = 3,
            NewMonth = 4,
            NewYear = 5
        }

        /// <summary>
        /// In UTC time.
        /// </summary>
        /// <param name="value"></param>
        public TimerMessage(TimerEventType value, DateTime dateTimeUtc) : base(value)
        {
            DateTimeUtc = dateTimeUtc;
        }

        public DateTime DateTimeUtc { get; }
    }
}
