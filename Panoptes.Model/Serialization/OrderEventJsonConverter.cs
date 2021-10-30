using QuantConnect;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Serialization;
using QuantConnect.Securities;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Panoptes.Model.Serialization
{
    public class OrderEventJsonConverter : JsonConverter<OrderEvent>
    {
        public override OrderEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                string id = null; // $"{AlgorithmId}-{OrderId}-{OrderEventId}";
                string algorithm_id = null;
                int order_id = 0;
                int order_event_id = 0;
                Symbol symbol = null;
                double time = 0;
                OrderStatus status = OrderStatus.None;
                decimal fill_price = 0;
                string fill_price_currency = null;
                decimal fill_quantity = 0;
                OrderDirection direction = OrderDirection.Hold;
                bool is_assignment = false;
                decimal quantity = 0;
                decimal order_fee_amount = 0;
                string order_fee_currency = null;
                string message = "";
                decimal stopPrice = 0;
                decimal limitPrice = 0;

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        switch (reader.GetString())
                        {
                            case "id":
                                reader.Read();
                                id = reader.GetString();
                                break;

                            case "algorithm-id":
                                reader.Read();
                                algorithm_id = reader.GetString();
                                break;

                            case "order-id":
                                reader.Read();
                                order_id = reader.GetInt32();
                                break;

                            case "order-event-id":
                                reader.Read();
                                order_event_id = reader.GetInt32();
                                break;

                            case "symbol":
                                reader.Read();
                                var symbolStr = reader.GetString();
                                var sid = SecurityIdentifier.Parse(symbolStr);
                                symbol = new Symbol(sid, sid.Symbol);
                                break;

                            case "time":
                                reader.Read();
                                time = reader.GetDouble();
                                break;

                            case "status":
                                reader.Read();
                                status = Enum.Parse<OrderStatus>(reader.GetString(), true);
                                break;

                            case "fill-price":
                                reader.Read();
                                fill_price = reader.GetDecimal();
                                break;

                            case "fill-price-currency":
                                reader.Read();
                                fill_price_currency = reader.GetString();
                                break;

                            case "fill-quantity":
                                reader.Read();
                                fill_quantity = reader.GetDecimal();
                                break;

                            case "direction":
                                reader.Read();
                                direction = Enum.Parse<OrderDirection>(reader.GetString(), true);
                                break;

                            case "is-assignment":
                                reader.Read();
                                is_assignment = reader.GetBoolean();
                                break;

                            case "quantity":
                                reader.Read();
                                quantity = reader.GetDecimal();
                                break;

                            case "order-fee-amount":
                                reader.Read();
                                order_fee_amount = reader.GetDecimal();
                                break;

                            case "order-fee-currency":
                                reader.Read();
                                order_fee_currency = reader.GetString();
                                break;

                            case "message":
                                reader.Read();
                                message = reader.GetString();
                                break;

                            case "stop-price":
                                reader.Read();
                                stopPrice = reader.GetDecimal();
                                break;

                            case "limit-price":
                                reader.Read();
                                limitPrice = reader.GetDecimal();
                                break;

                            default:
                                throw new ArgumentOutOfRangeException(reader.GetString());
                        }
                    }
                }

                var dt = DateTime.SpecifyKind(Time.UnixTimeStampToDateTime(time), DateTimeKind.Utc);

                var orderEvent = new OrderEvent(order_id, symbol, dt, status, direction, fill_price, fill_quantity,
                    new OrderFee(new CashAmount(order_fee_amount, order_fee_currency)), message);

                var serializedOrderEvent = new SerializedOrderEvent(orderEvent, algorithm_id)
                {
                    IsAssignment = is_assignment,
                    FillPriceCurrency = fill_price_currency,
                    LimitPrice = limitPrice,
                    StopPrice = stopPrice,
                    Quantity = quantity
                };

                return OrderEvent.FromSerialized(serializedOrderEvent);
            }
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, OrderEvent value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
