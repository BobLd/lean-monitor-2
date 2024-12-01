using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuantConnect.Orders;
using System;

namespace Panoptes.Model.Serialization
{
    public class CustomContractResolver : DefaultContractResolver
    {
        private readonly JsonConverter _orderConverter;

        public CustomContractResolver(JsonConverter orderConverter)
        {
            _orderConverter = orderConverter;
        }

        protected override JsonContract CreateContract(Type objectType)
        {
            if (typeof(Order).IsAssignableFrom(objectType))
            {
                var contract = base.CreateObjectContract(objectType);
                contract.Converter = _orderConverter;
                return contract;
            }

            return base.CreateContract(objectType);
        }
    }
}
