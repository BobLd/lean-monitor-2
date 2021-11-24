using System.Collections.Generic;

namespace Panoptes.Model.Charting
{
    public sealed class ChartDefinition
    {
        public string Name { get; set; }

        public Dictionary<string, SeriesDefinition> Series = new Dictionary<string, SeriesDefinition>();
    }
}
