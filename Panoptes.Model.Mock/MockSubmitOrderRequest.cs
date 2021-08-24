using QuantConnect;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using System;

namespace Panoptes.Model.Mock
{
    public class MockSubmitOrderRequest : SubmitOrderRequest
    {
        public MockSubmitOrderRequest(int id, OrderType orderType, SecurityType securityType, Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, DateTime time, string tag, IOrderProperties properties = null)
            : base(orderType, securityType, symbol, quantity, stopPrice, limitPrice, time, tag, properties)
        {
            this.OrderId = id;
        }

        public MockSubmitOrderRequest(int id, OrderType orderType, SecurityType securityType, Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, decimal triggerPrice, DateTime time, string tag, IOrderProperties properties = null)
            : base(orderType, securityType, symbol, quantity, stopPrice, limitPrice, triggerPrice, time, tag, properties)
        {
            this.OrderId = id;
        }
    }
}
