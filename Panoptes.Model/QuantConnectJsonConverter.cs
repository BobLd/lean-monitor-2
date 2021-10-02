/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect;
using QuantConnect.Orders;

// in namespace QuantConnect.Report

namespace Panoptes.Model
{
    /// <summary>
    /// Removes null values in the <see cref="Result"/> object's x,y values so that
    /// deserialization can occur without exceptions.
    /// </summary>
    /// <typeparam name="T">Result type to deserialize into</typeparam>
    public class NullResultValueTypeJsonConverter<T> : JsonConverter
        where T : QuantConnect.Result
    {
        private JsonSerializerSettings _settings;

        /// <summary>
        /// Initialize a new instance of <see cref="NullResultValueTypeJsonConverter{T}"/>
        /// </summary>
        public NullResultValueTypeJsonConverter()
        {
            _settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new OrderTypeNormalizingJsonConverter() },
                FloatParseHandling = FloatParseHandling.Decimal
            };
        }

        /// <summary>
        /// Determine if this converter can convert a given type
        /// </summary>
        /// <param name="objectType">Object type to convert</param>
        /// <returns>Always true</returns>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <summary>
        /// Read Json for conversion
        /// </summary>
        /// <returns>Resulting object</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            if (token.Type == JTokenType.Null)
            {
                return null;
            }

            foreach (JProperty property in token["Charts"].Children())
            {
                foreach (JProperty seriesProperty in property.Value["Series"])
                {
                    var newValues = new List<JToken>();
                    foreach (var entry in seriesProperty.Value["Values"])
                    {
                        if (entry["x"] == null || entry["x"].Value<long?>() == null ||
                            entry["y"] == null || entry["y"].Value<decimal?>() == null)
                        {
                            continue;
                        }

                        newValues.Add(entry);
                    }

                    token["Charts"][property.Name]["Series"][seriesProperty.Name]["Values"] = JArray.FromObject(newValues);
                }
            }

            // Deserialize with OrderJsonConverter, otherwise it will fail. We convert the token back
            // to its JSON representation and use the `JsonConvert.DeserializeObject<T>(...)` method instead
            // of using `token.ToObject<T>()` since it can be provided a JsonConverter in its arguments.
            return JsonConvert.DeserializeObject<T>(token.ToString(), _settings);
        }

        /// <summary>
        /// Write Json; Not implemented
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Normalizes the "Type" field to a value that will allow for
    /// successful deserialization in the <see cref="OrderJsonConverter"/> class.
    /// </summary>
    /// <example>
    /// All of these values should result in the same object:
    /// <code>
    /// [
    ///     { "Type": "marketOnOpen", ... },
    ///     { "Type": "MarketOnOpen", ... },
    ///     { "Type": 4, ... },
    /// ]
    /// </code>
    /// </example>
    public class OrderTypeNormalizingJsonConverter : JsonConverter
    {
        /// <summary>
        /// Determine if this Converter can convert a given object type
        /// </summary>
        /// <param name="objectType">Object type to convert</param>
        /// <returns>True if assignable from <see cref="Order"/></returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Order).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Read Json and convert
        /// </summary>
        /// <returns>Resulting <see cref="Order"/></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            // Takes the Type field and selects the correct OrderType instance
            var orderTypeValue = token["Type"].Value<string>();
            int orderTypeNumber;
            var orderType = Parse.TryParse(orderTypeValue, NumberStyles.Any, out orderTypeNumber) ?
                orderTypeNumber :
                (int)(OrderType)Enum.Parse(typeof(OrderType), orderTypeValue, true);

            token["Type"] = orderType;
            return OrderJsonConverter.CreateOrderFromJObject((JObject)token);
        }

        /// <summary>
        /// Write Json; Not implemented
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
