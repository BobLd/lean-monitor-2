﻿/*
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

// https://github.com/QuantConnect/Lean/blob/master/Common/Packets/SystemDebugPacket.cs

using PacketType = QuantConnect.Packets.PacketType;

namespace Panoptes.Model.Serialization.Packets
{
    /// <summary>
    /// Debug packets generated by Lean
    /// </summary>
    public class SystemDebugPacket : DebugPacket
    {
        /// <summary>
        /// Default constructor for JSON
        /// </summary>
        public SystemDebugPacket()
            : base(PacketType.SystemDebug)
        { }

        /// <summary>
        /// Create a new instance of the system debug packet
        /// </summary>
        public SystemDebugPacket(int projectId, string algorithmId, string compileId, string message, bool toast = false)
            : base(PacketType.SystemDebug)
        {
            ProjectId = projectId;
            Message = message;
            CompileId = compileId;
            AlgorithmId = algorithmId;
            Toast = toast;
        }
    }
}
