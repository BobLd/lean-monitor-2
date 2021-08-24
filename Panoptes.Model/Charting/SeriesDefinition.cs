using QuantConnect;
using System.Collections.Generic;
using System.Drawing;

namespace Panoptes.Model.Charting
{
    public class SeriesDefinition
    {
        public string Name { get; set; }

        public List<InstantChartPoint> Values { get; set; } = new List<InstantChartPoint>();

        public SeriesType SeriesType { get; set; } = SeriesType.Line;

        public Color Color { get; set; } = Color.CornflowerBlue;

        public string Unit { get; set; } = "?";

        public ScatterMarkerSymbol ScatterMarkerSymbol { get; set; } = ScatterMarkerSymbol.None;

        public int Index { get; set; }
    }
}
