using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuantConnect;

namespace Panoptes.Model.Serialization;

//https://github.com/QuantConnect/Lean/blob/master/Common/Util/CandlestickJsonConverter.cs
public class CandlestickJsonConverter : JsonConverter<Candlestick>
{
	public override Candlestick Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
		{
			var root = doc.RootElement;
			if (root.ValueKind == JsonValueKind.Object)
			{
				// Deserialize as ChartPoint and then create a Candlestick
				var chartPoint = JsonSerializer.Deserialize<ChartPoint>(root.GetRawText());
				if (chartPoint == null)
				{
					return null;
				}
				return new Candlestick(chartPoint.X, chartPoint.Y, chartPoint.Y, chartPoint.Y, chartPoint.Y);
			}
			else if (root.ValueKind == JsonValueKind.Array)
			{
				var array = root.EnumerateArray().ToArray();
				long time = array[0].GetInt64();
				decimal open = array[1].GetDecimal();
				decimal high = array[2].GetDecimal();
				decimal low = array[3].GetDecimal();
				decimal close = array[4].GetDecimal();
				return new Candlestick(time, open, high, low, close);
			}
		}

		return null;
	}

	public override void Write(Utf8JsonWriter writer, Candlestick value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}