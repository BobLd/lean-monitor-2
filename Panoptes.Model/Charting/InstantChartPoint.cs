using System;

namespace Panoptes.Model.Charting
{
    public class InstantChartPoint : IInstantChartPoint
    {
        public DateTimeOffset X { get; set; }
        public decimal Y { get; set; }

        public InstantChartPoint() { }

        public InstantChartPoint(DateTimeOffset x, decimal y)
        {
            X = x;
            Y = y;
        }
    }
}
