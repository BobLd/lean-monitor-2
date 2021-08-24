using NodaTime;

namespace Panoptes.Model.Charting
{
    public interface IInstantChartPoint
    {
        Instant X { get; set; }
    }
}
