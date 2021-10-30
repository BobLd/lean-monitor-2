using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Panoptes.Model.Serialization
{
    // https://github.com/QuantConnect/Lean/blob/master/Common/Orders/OrderJsonConverter.cs
    public class OrderJsonConverter : JsonConverter<Order>
    {
        public override Order Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jObject = JsonDocument.ParseValue(ref reader); //JObject.Load(reader);

            var order = CreateOrderFromJObject(jObject);

            return order;
        }

        public override void Write(Utf8JsonWriter writer, Order value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public static Order CreateOrderFromJObject(JsonDocument jObject)
        {
            throw new NotImplementedException();
        }

        /*
        /// <summary>
        /// Create an order from a simple JObject
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns>Order Object</returns>
        public static Order CreateOrderFromJObject(JObject jObject)
        {
            // create order instance based on order type field
            var orderType = (OrderType)jObject["Type"].Value<int>();
            var order = CreateOrder(orderType, jObject);

            // populate common order properties
            order.Id = jObject["Id"].Value<int>();

            var jsonStatus = jObject["Status"];
            var jsonTime = jObject["Time"];
            if (jsonStatus.Type == JTokenType.Integer)
            {
                order.Status = (OrderStatus)jsonStatus.Value<int>();
            }
            else if (jsonStatus.Type == JTokenType.Null)
            {
                order.Status = OrderStatus.Canceled;
            }
            else
            {
                // The `Status` tag can sometimes appear as a string of the enum value in the LiveResultPacket.
                order.Status = (OrderStatus)Enum.Parse(typeof(OrderStatus), jsonStatus.Value<string>(), true);
            }
            if (jsonTime != null && jsonTime.Type != JTokenType.Null)
            {
                order.Time = jsonTime.Value<DateTime>();
            }
            else
            {
                // `Time` can potentially be null in some LiveResultPacket instances, but
                // `CreatedTime` will always be there if `Time` is absent.
                order.Time = jObject["CreatedTime"].Value<DateTime>();
            }

            var orderSubmissionData = jObject["OrderSubmissionData"];
            if (orderSubmissionData != null && orderSubmissionData.Type != JTokenType.Null)
            {
                var bidPrice = orderSubmissionData["BidPrice"].Value<decimal>();
                var askPrice = orderSubmissionData["AskPrice"].Value<decimal>();
                var lastPrice = orderSubmissionData["LastPrice"].Value<decimal>();
                order.OrderSubmissionData = new OrderSubmissionData(bidPrice, askPrice, lastPrice);
            }

            var lastFillTime = jObject["LastFillTime"];
            var lastUpdateTime = jObject["LastUpdateTime"];
            var canceledTime = jObject["CanceledTime"];

            if (canceledTime != null && canceledTime.Type != JTokenType.Null)
            {
                order.CanceledTime = canceledTime.Value<DateTime>();
            }
            if (lastFillTime != null && lastFillTime.Type != JTokenType.Null)
            {
                order.LastFillTime = lastFillTime.Value<DateTime>();
            }
            if (lastUpdateTime != null && lastUpdateTime.Type != JTokenType.Null)
            {
                order.LastUpdateTime = lastUpdateTime.Value<DateTime>();
            }
            var tag = jObject["Tag"];
            if (tag != null && tag.Type != JTokenType.Null)
            {
                order.Tag = tag.Value<string>();
            }
            else
            {
                order.Tag = "";
            }

            order.Quantity = jObject["Quantity"].Value<decimal>();
            var orderPrice = jObject["Price"];
            if (orderPrice != null && orderPrice.Type != JTokenType.Null)
            {
                order.Price = orderPrice.Value<decimal>();
            }
            else
            {
                order.Price = default(decimal);
            }

            var priceCurrency = jObject["PriceCurrency"];
            if (priceCurrency != null && priceCurrency.Type != JTokenType.Null)
            {
                order.PriceCurrency = priceCurrency.Value<string>();
            }
            var securityType = (SecurityType)jObject["SecurityType"].Value<int>();
            order.BrokerId = jObject["BrokerId"].Select(x => x.Value<string>()).ToList();
            order.ContingentId = jObject["ContingentId"].Value<int>();

            var timeInForce = jObject["Properties"]?["TimeInForce"] ?? jObject["TimeInForce"] ?? jObject["Duration"];
            order.Properties.TimeInForce = timeInForce != null
                ? CreateTimeInForce(timeInForce, jObject)
                : TimeInForce.GoodTilCanceled;

            string market = null;

            //does data have market?
            var suppliedMarket = jObject.SelectTokens("Symbol.ID.Market");
            if (suppliedMarket.Any())
            {
                market = suppliedMarket.Single().Value<string>();
            }

            if (jObject.SelectTokens("Symbol.ID").Any())
            {
                var sid = SecurityIdentifier.Parse(jObject.SelectTokens("Symbol.ID").Single().Value<string>());
                var ticker = jObject.SelectTokens("Symbol.Value").Single().Value<string>();
                order.Symbol = new Symbol(sid, ticker);
            }
            else if (jObject.SelectTokens("Symbol.Value").Any())
            {
                // provide for backwards compatibility
                var ticker = jObject.SelectTokens("Symbol.Value").Single().Value<string>();

                if (market == null && !SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(ticker, securityType, out market))
                {
                    market = DefaultBrokerageModel.DefaultMarketMap[securityType];
                }
                order.Symbol = Symbol.Create(ticker, securityType, market);
            }
            else
            {
                var tickerstring = jObject["Symbol"].Value<string>();

                if (market == null && !SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(tickerstring, securityType, out market))
                {
                    market = DefaultBrokerageModel.DefaultMarketMap[securityType];
                }
                order.Symbol = Symbol.Create(tickerstring, securityType, market);
            }

            return order;
        }

        /// <summary>
        /// Creates an order of the correct type
        /// </summary>
        private static Order CreateOrder(OrderType orderType, JObject jObject)
        {
            Order order;
            switch (orderType)
            {
                case OrderType.Market:
                    order = new MarketOrder();
                    break;

                case OrderType.Limit:
                    order = new LimitOrder { LimitPrice = jObject["LimitPrice"] == null ? default(decimal) : jObject["LimitPrice"].Value<decimal>() };
                    break;

                case OrderType.StopMarket:
                    order = new StopMarketOrder
                    {
                        StopPrice = jObject["StopPrice"] == null ? default(decimal) : jObject["StopPrice"].Value<decimal>()
                    };
                    break;

                case OrderType.StopLimit:
                    order = new StopLimitOrder
                    {
                        LimitPrice = jObject["LimitPrice"] == null ? default(decimal) : jObject["LimitPrice"].Value<decimal>(),
                        StopPrice = jObject["StopPrice"] == null ? default(decimal) : jObject["StopPrice"].Value<decimal>()
                    };
                    break;

                case OrderType.LimitIfTouched:
                    order = new LimitIfTouchedOrder
                    {
                        LimitPrice = jObject["LimitPrice"] == null ? default(decimal) : jObject["LimitPrice"].Value<decimal>(),
                        TriggerPrice = jObject["TriggerPrice"] == null ? default(decimal) : jObject["TriggerPrice"].Value<decimal>()
                    };
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

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return order;
        }

        /// <summary>
        /// Creates a Time In Force of the correct type
        /// </summary>
        private static TimeInForce CreateTimeInForce(JToken timeInForce, JObject jObject)
        {
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
                        var expiry = jObject["DurationValue"].Value<DateTime>();
                        return TimeInForce.GoodTilDate(expiry);

                    default:
                        throw new Exception($"Unknown time in force value: {value}");
                }
            }

            // convert with TimeInForceJsonConverter
            return timeInForce.ToObject<TimeInForce>();
        }
        */
    }
}
