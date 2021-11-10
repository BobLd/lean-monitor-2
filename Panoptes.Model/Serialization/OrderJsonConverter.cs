using QuantConnect;
using QuantConnect.Brokerages;
using QuantConnect.Orders;
using QuantConnect.Orders.Serialization;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panoptes.Model.Serialization
{
    // https://github.com/QuantConnect/Lean/blob/master/Common/Orders/OrderJsonConverter.cs
    public sealed class OrderJsonConverter : JsonConverter<Order>
    {
        public override Order Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jObject = JsonDocument.ParseValue(ref reader);
           
            return CreateOrderFromJObject(jObject);
        }

        public override void Write(Utf8JsonWriter writer, Order value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an order from a simple JObject
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns>Order Object</returns>
        public static Order CreateOrderFromJObject(JsonDocument jObject)
        {
            // create order instance based on order type field
            var orderType = (OrderType)jObject.RootElement.GetProperty("Type").GetInt32();
            var order = CreateOrder(orderType, jObject);

            // populate common order properties
            order.OrderId = jObject.RootElement.GetProperty("Id").GetInt32(); // order.Id

            var jsonStatus = jObject.RootElement.GetProperty("Status");
            if (jsonStatus.ValueKind == JsonValueKind.Number)
            {
                order.Status = (OrderStatus)jsonStatus.GetInt32();
            }
            else if (jsonStatus.ValueKind == JsonValueKind.Null)
            {
                order.Status = OrderStatus.Canceled;
            }
            else
            {
                // The `Status` tag can sometimes appear as a string of the enum value in the LiveResultPacket.
                order.Status = (OrderStatus)Enum.Parse(typeof(OrderStatus), jsonStatus.GetString(), true);
            }

            if (jObject.RootElement.TryGetProperty("Time", out var jsonTime) && jsonTime.ValueKind != JsonValueKind.Null) // jsonTime != null && 
            {
                order.CreatedTime = Time.DateTimeToUnixTimeStamp(jsonTime.GetDateTime());
            }
            else
            {
                // `Time` can potentially be null in some LiveResultPacket instances, but
                // `CreatedTime` will always be there if `Time` is absent.
                order.CreatedTime = Time.DateTimeToUnixTimeStamp(jObject.RootElement.GetProperty("CreatedTime").GetDateTime());
            }

            OrderSubmissionData osd; // order.OrderSubmissionData
            if (jObject.RootElement.TryGetProperty("OrderSubmissionData", out var orderSubmissionData) && orderSubmissionData.ValueKind != JsonValueKind.Null)
            {
                order.SubmissionBidPrice = orderSubmissionData.GetProperty("BidPrice").GetDecimal();
                order.SubmissionAskPrice = orderSubmissionData.GetProperty("AskPrice").GetDecimal();
                order.SubmissionLastPrice = orderSubmissionData.GetProperty("LastPrice").GetDecimal();
            }

            if (jObject.RootElement.TryGetProperty("CanceledTime", out var canceledTime) && canceledTime.ValueKind != JsonValueKind.Null)
            {
                order.CanceledTime = Time.DateTimeToUnixTimeStamp(canceledTime.GetDateTime());
            }

            if (jObject.RootElement.TryGetProperty("LastFillTime", out var lastFillTime) && lastFillTime.ValueKind != JsonValueKind.Null)
            {
                order.LastFillTime = Time.DateTimeToUnixTimeStamp(lastFillTime.GetDateTime());
            }

            if (jObject.RootElement.TryGetProperty("LastUpdateTime", out var lastUpdateTime) && lastUpdateTime.ValueKind != JsonValueKind.Null)
            {
                order.LastUpdateTime = Time.DateTimeToUnixTimeStamp(lastUpdateTime.GetDateTime());
            }

            if (jObject.RootElement.TryGetProperty("Tag", out var tag) && tag.ValueKind != JsonValueKind.Null)
            {
                order.Tag = tag.GetString();
            }

            order.Quantity = jObject.RootElement.GetProperty("Quantity").GetDecimal(); // order.Quantity
            if (jObject.RootElement.TryGetProperty("Price", out var orderPrice) && orderPrice.ValueKind != JsonValueKind.Null)
            {
                order.Price = orderPrice.GetDecimal();
            }

            if (jObject.RootElement.TryGetProperty("PriceCurrency", out var priceCurrency) && priceCurrency.ValueKind != JsonValueKind.Null)
            {
                order.PriceCurrency = priceCurrency.GetString();
            }

            var securityType = (SecurityType)jObject.RootElement.GetProperty("SecurityType").GetInt32();
            order.BrokerId = jObject.RootElement.GetProperty("BrokerId").EnumerateArray().Select(x => x.GetString()).ToList();
            order.ContingentId = jObject.RootElement.GetProperty("ContingentId").GetInt32();

            //var timeInForce = jObject["Properties"]?["TimeInForce"] ?? jObject["TimeInForce"] ?? jObject["Duration"];
            JsonElement? timeInForce = null;
            if (jObject.RootElement.TryGetProperty("Properties", out var property) && property.TryGetProperty("TimeInForce", out var tif))
            {
                timeInForce = tif;
            }
            else if (jObject.RootElement.TryGetProperty("TimeInForce", out tif))
            {
                timeInForce = tif;
            }
            else if (jObject.RootElement.TryGetProperty("Duration", out tif))
            {
                timeInForce = tif;
            }

            //  order.Properties.TimeInForce 
            var objTimeInForce = timeInForce.HasValue ? CreateTimeInForce(timeInForce.Value, jObject) : TimeInForce.GoodTilCanceled;

            var timeInForceType = objTimeInForce.GetType().Name;
            // camelcase the type name, lowering the first char
            order.TimeInForceType = char.ToLowerInvariant(timeInForceType[0]) + timeInForceType.Substring(1);
            if (objTimeInForce is GoodTilDateTimeInForce goodTilDate)
            {
                var expiry = goodTilDate.Expiry;
                order.TimeInForceExpiry = Time.DateTimeToUnixTimeStamp(expiry);
            }

            string market = null;

            //does data have market?
            //Symbol symbol; // order.Symbol

            if (jObject.RootElement.TryGetProperty("Symbol", out var sm) &&
                sm.ValueKind == JsonValueKind.Object && sm.TryGetProperty("ID", out var im) &&
                im.ValueKind == JsonValueKind.Object && im.TryGetProperty("Market", out var suppliedMarket))
            {
                market = suppliedMarket.GetString();
            }

            if (jObject.RootElement.TryGetProperty("Symbol", out var s))
            {
                if (s.ValueKind == JsonValueKind.Object)
                {
                    if (s.TryGetProperty("ID", out var i))
                    {
                        var sid = SecurityIdentifier.Parse(i.GetString()); //jObject.SelectTokens("Symbol.ID").Single().Value<string>());
                        var ticker = s.GetProperty("Value").GetString(); //jObject.SelectTokens("Symbol.Value").Single().Value<string>();
                        order.Symbol = new Symbol(sid, ticker).ID.ToString();
                    }
                    else if (s.TryGetProperty("Value", out var v))
                    {
                        // provide for backwards compatibility
                        var ticker = v.GetString(); //jObject.SelectTokens("Symbol.Value").Single().Value<string>();

                        if (market == null && !SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(ticker, securityType, out market))
                        {
                            market = DefaultBrokerageModel.DefaultMarketMap[securityType];
                        }
                        order.Symbol = Symbol.Create(ticker, securityType, market).ID.ToString();
                    }
                }
                else
                {
                    var tickerstring = s.GetString(); //jObject["Symbol"].Value<string>();

                    if (market == null && !SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(tickerstring, securityType, out market))
                    {
                        market = DefaultBrokerageModel.DefaultMarketMap[securityType];
                    }
                    order.Symbol = Symbol.Create(tickerstring, securityType, market).ID.ToString();
                }
            }

            var orderFinal = Order.FromSerialized(order);

            return orderFinal;
        }

        /// <summary>
        /// Creates an order of the correct type
        /// </summary>
        private static SerializedOrder CreateOrder(OrderType orderType, JsonDocument jObject)
        {
            SerializedOrder serializedOrder;

            decimal GetPropery(string name)
            {
                if (jObject.RootElement.TryGetProperty(name, out var property))
                {
                    return property.GetDecimal();
                }
                //throw new ArgumentException("Property not found", nameof(name));
                return default;
            }

            switch (orderType)
            {
                case OrderType.Market:
                    serializedOrder = new SerializedOrder(new MarketOrder(), "");
                    break;

                case OrderType.Limit:
                    serializedOrder = new SerializedOrder(new LimitOrder(), "");
                    serializedOrder.LimitPrice = GetPropery("LimitPrice");
                    break;

                case OrderType.StopMarket:
                    serializedOrder = new SerializedOrder(new StopMarketOrder(), "");
                    serializedOrder.StopPrice = GetPropery("StopPrice");
                    break;

                case OrderType.StopLimit:
                    serializedOrder = new SerializedOrder(new StopLimitOrder(), "");
                    serializedOrder.StopPrice = GetPropery("StopPrice");
                    serializedOrder.LimitPrice = GetPropery("LimitPrice");
                    break;

                case OrderType.LimitIfTouched:
                    serializedOrder = new SerializedOrder(new LimitIfTouchedOrder(), "");
                    serializedOrder.LimitPrice = GetPropery("LimitPrice");
                    serializedOrder.TriggerPrice = GetPropery("TriggerPrice");
                    break;

                case OrderType.MarketOnOpen:
                    serializedOrder = new SerializedOrder(new MarketOnOpenOrder(), "");
                    break;

                case OrderType.MarketOnClose:
                    serializedOrder = new SerializedOrder(new MarketOnCloseOrder(), "");
                    break;

                case OrderType.OptionExercise:
                    serializedOrder = new SerializedOrder(new OptionExerciseOrder(), "");
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Unknown order type '{orderType}'.");
            }

            return serializedOrder;
        }

        /// <summary>
        /// Creates a Time In Force of the correct type
        /// </summary>
        private static TimeInForce CreateTimeInForce(JsonElement timeInForce, JsonDocument jObject)
        {
            return TimeInForce.GoodTilCanceled;
            /*
            // for backward-compatibility support deserialization of old JSON format
            if (timeInForce is JValue)
            {
                var value = timeInForce.Value<int>();

                switch (value)
                {
                    case 0:
                        return TimeInForce.GoodTilCanceled;

                    case 1:
                        return TimeInForce.Day;

                    case 2:
                        var expiry = jObject.RootElement.GetProperty("DurationValue").GetDateTime();
                        return TimeInForce.GoodTilDate(expiry);

                    default:
                        throw new Exception($"Unknown time in force value: {value}");
                }
            }

            // convert with TimeInForceJsonConverter
            return timeInForce.ToObject<TimeInForce>();
            */
        }
    }
}
