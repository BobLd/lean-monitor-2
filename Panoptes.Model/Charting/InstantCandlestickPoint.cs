using System;

namespace Panoptes.Model.Charting
{
    public class InstantCandlestickPoint : IInstantChartPoint
    {
        public DateTimeOffset X { get; set; }

        public decimal Open { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Close { get; set; }

        public decimal Volume { get; set; }

        public decimal Y => Close;
    }
}
