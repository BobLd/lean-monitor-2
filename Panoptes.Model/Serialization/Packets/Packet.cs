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
 *
*/

// https://github.com/QuantConnect/Lean/blob/master/Common/Packets/Packet.cs

using System.Text.Json.Serialization;
using PacketType = QuantConnect.Packets.PacketType;

namespace Panoptes.Model.Serialization.Packets
{
    /// <summary>
    /// Base class for packet messaging system
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// Packet type defined by a string enum
        /// </summary>
        [JsonPropertyName("eType")]
        public PacketType Type { get; set; } = PacketType.None;

        /// <summary>
        /// User unique specific channel endpoint to send the packets
        /// </summary>
        [JsonPropertyName("sChannel")]
        public virtual string Channel { get; set; } = "";

        /// <summary>
        /// Initialize the base class and setup the packet type.
        /// </summary>
        /// <param name="type">PacketType for the class.</param>
        public Packet(PacketType type)
        {
            Channel = "";
            Type = type;
        }
    }
}
