using QuantConnect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Panoptes.Model.Serialization
{

	//https://github.com/QuantConnect/Lean/blob/master/Common/Util/SeriesJsonConverter.cs
	public sealed class SeriesJsonConverter : JsonConverter<BaseSeries>
	{
		public override BaseSeries Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
			{
				var root = doc.RootElement;

				string name = root.GetProperty("Name").GetString();
				string unit = root.GetProperty("Unit").GetString();
				int index = root.GetProperty("Index").GetInt32();
				SeriesType seriesType = (SeriesType)root.GetProperty("SeriesType").GetInt32();

				if (seriesType == SeriesType.Candle)
				{
					var candlestickSeries = new CandlestickSeries
					{
						Name = name,
						Unit = unit,
						Index = index,
						SeriesType = seriesType
					};

					// Deserialize Values into List<Candlestick>
					var values = JsonSerializer.Deserialize<List<Candlestick>>(root.GetProperty("Values").GetRawText(), DefaultJsonSerializerOptions.Default);
					candlestickSeries.Values = values.Where(x => x != null).Cast<ISeriesPoint>().ToList();

					return candlestickSeries;
				}
				else
				{
					var series = new Series
					{
						Name = name,
						Unit = unit,
						Index = index,
						SeriesType = seriesType
					};

					// Deserialize Values into List<ChartPoint>
					var values = JsonSerializer.Deserialize<List<ChartPoint>>(root.GetProperty("Values").GetRawText(), DefaultJsonSerializerOptions.Default);
					series.Values = values.Where(x => x != null).Cast<ISeriesPoint>().ToList();

					// Deserialize other properties if needed
					if (root.TryGetProperty("Color", out var colorProp))
					{
						series.Color = JsonSerializer.Deserialize<Color>(colorProp.GetRawText(), DefaultJsonSerializerOptions.Default);
					}

					if (root.TryGetProperty("ScatterMarkerSymbol", out var scatterMarkerSymbolProp))
					{
						series.ScatterMarkerSymbol = JsonSerializer.Deserialize<ScatterMarkerSymbol>(scatterMarkerSymbolProp.GetRawText(), DefaultJsonSerializerOptions.Default);
					}

					return series;
				}
			}
		}


		public override void Write(Utf8JsonWriter writer, BaseSeries value, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
		}
	}
}
