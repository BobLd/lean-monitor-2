using System;

namespace Panoptes.Model
{
    public static class Times
    {
        public static readonly TimeSpan Zero = TimeSpan.Zero;
        public static readonly TimeSpan OneMillisecond = TimeSpan.FromMilliseconds(1);
        public static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan OneHour = TimeSpan.FromHours(1);
        public static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

        public static double GetSecondsToNextMinute()
        {
            var timeOfDay = DateTime.UtcNow.TimeOfDay;
            var nextFullMinute = TimeSpan.FromMinutes(Math.Ceiling(timeOfDay.TotalMinutes));
            return (nextFullMinute - timeOfDay).TotalSeconds;
        }

        /// <summary>
        /// Round down to previous timespan.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static DateTime RoundDown(this DateTime dateTime, TimeSpan interval)
        {
            if (interval == TimeSpan.Zero)
            {
                // divide by zero exception
                return dateTime;
            }

            var amount = dateTime.Ticks % interval.Ticks;
            if (amount > 0)
            {
                return dateTime.AddTicks(-amount);
            }

            return dateTime;
        }

        /// <summary>
        /// Round down to previous timespan.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static double OxyplotRoundDown(double dateTime, TimeSpan interval)
        {
            return OxyplotToDouble(OxyplotToDateTime(dateTime).RoundDown(interval));
        }

        #region OxyPlot
        /// <summary>
        /// Converts a numeric representation of the date (number of days after the time origin) to a DateTime structure.
        /// </summary>
        /// <param name="value">The number of days after the time origin.</param>
        /// <returns>A <see cref="DateTime" /> structure. Ticks = 0 if the value is invalid.</returns>
        public static DateTime OxyplotToDateTime(double value)
        {
            if (double.IsNaN(value) || value < OxyplotMinDayValue || value > OxyplotMaxDayValue)
            {
                return default;
            }

            return OxyplotTimeOrigin.AddDays(value - 1);
        }

        /// <summary>
        /// Converts a DateTime to days after the time origin.
        /// </summary>
        /// <param name="value">The date/time structure.</param>
        /// <returns>The number of days after the time origin.</returns>
        public static double OxyplotToDouble(DateTime value)
        {
            var span = value - OxyplotTimeOrigin;
            return span.TotalDays + 1;
        }

        /// <summary>
        /// The time origin.
        /// </summary>
        /// <remarks>This gives the same numeric date values as Excel</remarks>
        private static readonly DateTime OxyplotTimeOrigin = new DateTime(1899, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// The maximum day value
        /// </summary>
        private static readonly double OxyplotMaxDayValue = (DateTime.MaxValue - OxyplotTimeOrigin).TotalDays;

        /// <summary>
        /// The minimum day value
        /// </summary>
        private static readonly double OxyplotMinDayValue = (DateTime.MinValue - OxyplotTimeOrigin).TotalDays;
        #endregion
    }
}
