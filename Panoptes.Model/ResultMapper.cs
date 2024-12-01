using Panoptes.Model.Charting;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Panoptes.Model
{
    // For many elements we use custom objects in this tool.
    public static class ResultMapper
    {
        public static Dictionary<string, ChartDefinition> MapToChartDefinitionDictionary(this IDictionary<string, Chart> sourceDictionary)
        {
            return sourceDictionary == null
                ? new Dictionary<string, ChartDefinition>()
                : sourceDictionary.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.MapToChartDefinition());
        }

        public static Dictionary<string, Chart> MapToChartDictionary(this IDictionary<string, ChartDefinition> sourceDictionary)
        {
            return sourceDictionary.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.MapToChart());
        }

        private static IInstantChartPoint MapToTimeStampChartPoint(this ISeriesPoint point)
        {
            if (point is ChartPoint chartPoint)
            {
                return new InstantChartPoint
                {
                    X = new DateTimeOffset(chartPoint.Time),
                    Y = chartPoint.y ?? 0m
                };
            }
            else if (point is Candlestick candlestick)
            {
                return new InstantCandlestickPoint
                {
                    X = new DateTimeOffset(candlestick.Time),
                    Open = candlestick.Open ?? 0m,
                    High = candlestick.High ?? 0m,
                    Low = candlestick.Low ?? 0m,
                    Close = candlestick.Close ?? 0m
                    // Volume = candlestick.Volume ?? 0m
                };
            }
            throw new NotSupportedException($"Unsupported ISeriesPoint type: {point.GetType().Name}");
        }

        private static ISeriesPoint MapToSeriesPoint(this IInstantChartPoint point)
        {
            if (point is InstantChartPoint chartPoint)
            {
                return new ChartPoint
                {
                    Time = chartPoint.X.DateTime,
                    y = chartPoint.Y
                };
            }
            else if (point is InstantCandlestickPoint candlestickPoint)
            {
                return new Candlestick
                {
                    Time = candlestickPoint.X.DateTime,
                    Open = candlestickPoint.Open,
                    High = candlestickPoint.High,
                    Low = candlestickPoint.Low,
                    Close = candlestickPoint.Close
                    // Volume = candlestickPoint.Volume
                };
            }
            throw new NotSupportedException($"Unsupported IInstantChartPoint type: {point.GetType().Name}");
        }

        private static ChartDefinition MapToChartDefinition(this Chart sourceChart)
        {
            return new ChartDefinition
            {
                Name = sourceChart.Name,
                Series = sourceChart.Series.MapToSeriesDefinitionDictionary()
            };
        }

        private static Chart MapToChart(this ChartDefinition sourceChart)
        {
            var chart = new Chart
            {
                Name = sourceChart.Name
            };

            foreach (var seriesDefinition in sourceChart.Series)
            {
                chart.Series.Add(seriesDefinition.Key, seriesDefinition.Value.MapToBaseSeries());
            }

            return chart;
        }

        private static Dictionary<string, SeriesDefinition> MapToSeriesDefinitionDictionary(this IDictionary<string, BaseSeries> sourceSeries)
        {
            var result = new Dictionary<string, SeriesDefinition>();

            foreach (var kvp in sourceSeries)
            {
                var seriesDefinition = kvp.Value.MapToSeriesDefinition();
                result[kvp.Key] = seriesDefinition;
            }

            return result;
        }

        private static SeriesDefinition MapToSeriesDefinition(this BaseSeries sourceSeries)
        {
            var seriesDefinition = new SeriesDefinition
            {
                Name = sourceSeries.Name,
                Unit = sourceSeries.Unit,
                Index = sourceSeries.Index,
                SeriesType = sourceSeries.SeriesType,
                Values = sourceSeries.Values.Select(v => v.MapToTimeStampChartPoint()).ToList()
            };

            if (sourceSeries is Series series)
            {
                seriesDefinition.Color = series.Color;
                seriesDefinition.ScatterMarkerSymbol = series.ScatterMarkerSymbol;
            }

            return seriesDefinition;
        }

        private static BaseSeries MapToBaseSeries(this SeriesDefinition sourceSeries)
        {
            var series = new Series(
                name: sourceSeries.Name,
                type: sourceSeries.SeriesType,
                index: sourceSeries.Index,
                unit: sourceSeries.Unit)
            {
                Color = sourceSeries.Color,
                ScatterMarkerSymbol = sourceSeries.ScatterMarkerSymbol,
                Values = sourceSeries.Values.Select(v => v.MapToSeriesPoint()).ToList()
            };

            return series;
        }
    }
}
