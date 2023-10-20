using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuantConnect;

namespace Panoptes.Model.Serialization;

//https://github.com/QuantConnect/Lean/blob/master/Common/Util/ChartPointJsonConverter.cs
public class ChartPointJsonConverter : JsonConverter<ChartPoint>
{
	public override ChartPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
		{
			var root = doc.RootElement;
			if (root.ValueKind == JsonValueKind.Object)
			{
				long x = root.GetProperty("x").GetInt64();
				if (!root.TryGetProperty("y", out var yProperty))
				{
					return new ChartPoint(x, 0M);
				}

				decimal y = yProperty.GetDecimal();
				return new ChartPoint(x, y);
			}
			else if (root.ValueKind == JsonValueKind.Array)
			{
				var array = root.EnumerateArray().ToArray();
				long x = array[0].GetInt64();
				decimal y = array[1].GetDecimal();
				return new ChartPoint(x, y);
			}
		}

		return null;
	}

	public override void Write(Utf8JsonWriter writer, ChartPoint value, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}
}