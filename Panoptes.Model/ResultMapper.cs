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
            return sourceDictionary == null ?
                new Dictionary<string, ChartDefinition>() :
                sourceDictionary.ToDictionary(entry => entry.Key, entry => MapToChartDefinition(entry.Value));
        }

        public static Dictionary<string, Chart> MapToChartDictionary(this IDictionary<string, ChartDefinition> sourceDictionary)
        {
            return sourceDictionary.ToDictionary(entry => entry.Key, entry => MapToChart(entry.Value));
        }

		private static InstantChartPoint MapToTimeStampChartPoint(this ISeriesPoint point)
		{
			if (point is ChartPoint chartPoint)
			{
				return new InstantChartPoint
				{
					X = DateTimeOffset.FromUnixTimeSeconds(chartPoint.x),
					Y = chartPoint.y
				};
			}
			else if (point is Candlestick candlestick)
			{
				decimal y = candlestick.Close;

				return new InstantChartPoint
				{
					X = DateTimeOffset.FromUnixTimeSeconds(candlestick.LongTime), 
					Y = y
				};
			}

			throw new NotImplementedException("Type not supported.");
		}


		private static ChartPoint MapToChartPoint(this InstantChartPoint point)
        {
            return new ChartPoint
            {
                // QuantConnect chartpoints are always in Unix TimeStamp (seconds)
                x = point.X.ToUnixTimeSeconds(),
                y = point.Y
            };
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
            return new Chart
            {
                Name = sourceChart.Name,
                Series = sourceChart.Series.MapToSeriesDictionary()
            };
        }

        private static Dictionary<string, SeriesDefinition> MapToSeriesDefinitionDictionary(this IDictionary<string, BaseSeries> sourceSeries)
        {
	        return sourceSeries.ToDictionary(
		        entry => entry.Key,
		        entry => entry.Value.MapToSeriesDefinition() 
	        );
        }


		private static Dictionary<string, BaseSeries> MapToSeriesDictionary(this IDictionary<string, SeriesDefinition> sourceSeries)
		{
			return sourceSeries.ToDictionary(entry => entry.Key, entry => (BaseSeries)entry.Value.MapToSeries());
		}


		private static SeriesDefinition MapToSeriesDefinition(this BaseSeries sourceSeries)
		{
			var definition = new SeriesDefinition
			{
				Index = sourceSeries.Index,
				Name = sourceSeries.Name,
				SeriesType = sourceSeries.SeriesType,
				Unit = sourceSeries.Unit,
				Values = sourceSeries.Values.ConvertAll(v => v.MapToTimeStampChartPoint())
			};

			if (sourceSeries is Series series)
			{
				definition.Color = series.Color;
				definition.ScatterMarkerSymbol = series.ScatterMarkerSymbol;
			}

			return definition;
		}


		private static Series MapToSeries(this SeriesDefinition sourceSeries)
        {
            return new Series
            {
                Color = sourceSeries.Color,
                Index = sourceSeries.Index,
                Name = sourceSeries.Name,
                ScatterMarkerSymbol = sourceSeries.ScatterMarkerSymbol,
                SeriesType = sourceSeries.SeriesType,
                Unit = sourceSeries.Unit,
				Values = sourceSeries.Values.ConvertAll(v => (ISeriesPoint)v.MapToChartPoint())

			};
        }
    }
}
