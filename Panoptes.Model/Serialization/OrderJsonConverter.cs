using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Orders;
using System;

public class OrderJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Order).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {

        JObject jo = JObject.Load(reader);

        var typeToken = jo["type"];
        if (typeToken == null)
        {
            throw new JsonSerializationException("Cannot deserialize Order without 'type' field.");
        }

        int typeInt = typeToken.Value<int>();

        if (!Enum.IsDefined(typeof(OrderType), typeInt))
        {
            throw new JsonSerializationException($"Unknown OrderType value: {typeInt}");
        }

        OrderType orderType = (OrderType)typeInt;

        Order order;
        switch (orderType)
        {
            case OrderType.Market:
                order = new MarketOrder();
                break;
            case OrderType.Limit:
                order = new LimitOrder();
                break;
            case OrderType.StopMarket:
                order = new StopMarketOrder();
                break;
            case OrderType.StopLimit:
                order = new StopLimitOrder();
                break;
            case OrderType.MarketOnOpen:
                order = new MarketOnOpenOrder();
                break;
            case OrderType.MarketOnClose:
                order = new MarketOnCloseOrder();
                break;
            case OrderType.OptionExercise:
                order = new OptionExerciseOrder();
                break;
            case OrderType.LimitIfTouched:
                order = new LimitIfTouchedOrder();
                break;
            case OrderType.ComboMarket:
                order = new ComboMarketOrder();
                break;
            case OrderType.ComboLimit:
                order = new ComboLimitOrder();
                break;
            case OrderType.ComboLegLimit:
                order = new ComboLegLimitOrder();
                break;
            case OrderType.TrailingStop:
                order = new TrailingStopOrder();
                break;
            default:
                throw new NotSupportedException($"Order type '{orderType}' is not supported.");
        }

        serializer.Populate(jo.CreateReader(), order);
        return order;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}

