using QuantConnect;
using System.Collections.Generic;
using System.Drawing;

namespace Panoptes.Model.Charting
{
    public sealed class SeriesDefinition
    {
        public string Name { get; set; }

        public string Unit { get; set; }

        public int Index { get; set; }

        public SeriesType SeriesType { get; set; }

        public List<IInstantChartPoint> Values { get; set; } = new List<IInstantChartPoint>();

        public string IndexName { get; set; }

        public int? ZIndex { get; set; }

        public string Tooltip { get; set; }

        public Color Color { get; set; } = Color.CornflowerBlue;

        public ScatterMarkerSymbol ScatterMarkerSymbol { get; set; } = ScatterMarkerSymbol.None;
    }
}
