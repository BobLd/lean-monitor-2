using System;

namespace Panoptes.Model.Charting
{
    public struct InstantChartPoint : IInstantChartPoint
    {
        public DateTimeOffset X { get; set; }

        public decimal Y { get; set; }

        public InstantChartPoint(DateTimeOffset x, decimal y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"{X} - {Y}";
        }
    }
}
