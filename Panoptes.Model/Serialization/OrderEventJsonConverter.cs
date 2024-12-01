using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;

namespace Panoptes.Model.Serialization
{
    public sealed class OrderEventJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(OrderEvent);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            // Use nullable types and check for missing required properties
            int? orderId = jo.Value<int?>("orderId");
            if (orderId == null)
                throw new JsonSerializationException("Missing required property 'orderId'.");

            int? orderEventId = jo.Value<int?>("orderEventId") ?? 0; // Default to 0 if not provided

            string symbolStr = jo.Value<string>("symbol");
            if (string.IsNullOrEmpty(symbolStr))
                throw new JsonSerializationException("Missing required property 'symbol'.");

            double? time = jo.Value<double?>("time");
            if (time == null)
                throw new JsonSerializationException("Missing required property 'time'.");

            string statusStr = jo.Value<string>("status");
            if (string.IsNullOrEmpty(statusStr))
                throw new JsonSerializationException("Missing required property 'status'.");

            decimal? fillPrice = jo.Value<decimal?>("fillPrice");
            if (fillPrice == null)
                throw new JsonSerializationException("Missing required property 'fillPrice'.");

            string fillPriceCurrency = jo.Value<string>("fillPriceCurrency");

            decimal? fillQuantity = jo.Value<decimal?>("fillQuantity");
            if (fillQuantity == null)
                throw new JsonSerializationException("Missing required property 'fillQuantity'.");

            string directionStr = jo.Value<string>("direction");
            if (string.IsNullOrEmpty(directionStr))
                throw new JsonSerializationException("Missing required property 'direction'.");

            bool isAssignment = jo.Value<bool?>("isAssignment") ?? false;
            decimal quantity = jo.Value<decimal?>("quantity") ?? 0m;
            decimal orderFeeAmount = jo.Value<decimal?>("orderFeeAmount") ?? 0m;
            string orderFeeCurrency = jo.Value<string>("orderFeeCurrency");
            string message = jo.Value<string>("message");
            decimal stopPrice = jo.Value<decimal?>("stopPrice") ?? 0m;
            decimal limitPrice = jo.Value<decimal?>("limitPrice") ?? 0m;

            // Parse the symbol
            var sid = SecurityIdentifier.Parse(symbolStr);
            var symbol = new Symbol(sid, sid.Symbol);

            // Parse enum values
            if (!Enum.TryParse<OrderStatus>(statusStr, true, out var status))
                throw new JsonSerializationException($"Invalid 'status' value: {statusStr}");

            if (!Enum.TryParse<OrderDirection>(directionStr, true, out var direction))
                throw new JsonSerializationException($"Invalid 'direction' value: {directionStr}");

            // Convert time
            var dateTime = Time.UnixTimeStampToDateTime(time.Value);

            // Create the OrderEvent object
            var orderEvent = new OrderEvent(orderId.Value, symbol, dateTime, status, direction, fillPrice.Value, fillQuantity.Value, new OrderFee(new CashAmount(orderFeeAmount, orderFeeCurrency)), message)
            {
                Id = orderEventId.Value,
                IsAssignment = isAssignment,
                FillPriceCurrency = fillPriceCurrency,
                LimitPrice = limitPrice,
                StopPrice = stopPrice,
                Quantity = quantity
            };

            return orderEvent;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Implement serialization if needed
            throw new NotImplementedException();
        }
    }
}
