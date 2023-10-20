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

// https://github.com/QuantConnect/Lean/blob/master/Common/Packets/BacktestResultPacket.cs

using QuantConnect;
using QuantConnect.Orders;
using QuantConnect.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using BacktestResultParameters = QuantConnect.Packets.BacktestResultParameters;
using PacketType = QuantConnect.Packets.PacketType;

namespace Panoptes.Model.Serialization.Packets
{
    /// <summary>
    /// Backtest result packet: send backtest information to GUI for user consumption.
    /// </summary>
    public class BacktestResultPacket : Packet
    {
        /// <summary>
        /// User Id placing this task
        /// </summary>
        [JsonPropertyName("iUserID")]
        public int UserId = 0;

        /// <summary>
        /// Project Id of the this task.
        /// </summary>
        [JsonPropertyName("iProjectID")]
        public int ProjectId = 0;

        /// <summary>
        /// User Session Id
        /// </summary>
        [JsonPropertyName("sSessionID")]
        public string SessionId = "";

        /// <summary>
        /// BacktestId for this result packet
        /// </summary>
        [JsonPropertyName("sBacktestID")]
        public string BacktestId = "";

        /// <summary>
        /// OptimizationId for this result packet if any
        /// </summary>
        [JsonPropertyName("sOptimizationID")]
        public string OptimizationId;

        /// <summary>
        /// Compile Id for the algorithm which generated this result packet.
        /// </summary>
        [JsonPropertyName("sCompileID")]
        public string CompileId = "";

        /// <summary>
        /// Start of the backtest period as defined in Initialize() method.
        /// </summary>
        [JsonPropertyName("dtPeriodStart")]
        public DateTime PeriodStart = DateTime.Now;

        /// <summary>
        /// End of the backtest period as defined in the Initialize() method.
        /// </summary>
        [JsonPropertyName("dtPeriodFinish")]
        public DateTime PeriodFinish = DateTime.Now;

        /// <summary>
        /// DateTime (EST) the user requested this backtest.
        /// </summary>
        [JsonPropertyName("dtDateRequested")]
        public DateTime DateRequested = DateTime.Now;

        /// <summary>
        /// DateTime (EST) when the backtest was completed.
        /// </summary>
        [JsonPropertyName("dtDateFinished")]
        public DateTime DateFinished = DateTime.Now;

        /// <summary>
        /// Progress of the backtest as a percentage from 0-1 based on the days lapsed from start-finish.
        /// </summary>
        [JsonPropertyName("dProgress")]
        public decimal Progress = 0;

        /// <summary>
        /// Name of this backtest.
        /// </summary>
        [JsonPropertyName("sName")]
        public string Name = String.Empty;

        /// <summary>
        /// Result data object for this backtest
        /// </summary>
        [JsonPropertyName("oResults")]
        public BacktestResult Results = new BacktestResult();

        /// <summary>
        /// Processing time of the algorithm (from moment the algorithm arrived on the algorithm node)
        /// </summary>
        [JsonPropertyName("dProcessingTime")]
        public double ProcessingTime = 0;

        /// <summary>
        /// Estimated number of tradeable days in the backtest based on the start and end date or the backtest
        /// </summary>
        [JsonPropertyName("iTradeableDates")]
        public int TradeableDates = 0;

        /// <summary>
        /// Default constructor for JSON Serialization
        /// </summary>
        public BacktestResultPacket()
            : base(PacketType.BacktestResult)
        { }

        /// <summary>
        /// Compose the packet from a JSON string:
        /// </summary>
        public BacktestResultPacket(string json)
            : base(PacketType.BacktestResult)
        {
            throw new NotImplementedException();
            /*
            try
            {
                var packet = JsonConvert.DeserializeObject<BacktestResultPacket>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                CompileId = packet.CompileId;
                Channel = packet.Channel;
                PeriodFinish = packet.PeriodFinish;
                PeriodStart = packet.PeriodStart;
                Progress = packet.Progress;
                SessionId = packet.SessionId;
                BacktestId = packet.BacktestId;
                Type = packet.Type;
                UserId = packet.UserId;
                DateFinished = packet.DateFinished;
                DateRequested = packet.DateRequested;
                Name = packet.Name;
                ProjectId = packet.ProjectId;
                Results = packet.Results;
                ProcessingTime = packet.ProcessingTime;
                TradeableDates = packet.TradeableDates;
                OptimizationId = packet.OptimizationId;
            }
            catch (Exception err)
            {
                Log.Trace($"BacktestResultPacket(): Error converting json: {err}");
            }
            */
        }

        /// <summary>
        /// Compose result data packet - with tradable dates from the backtest job task and the partial result packet.
        /// </summary>
        /// <param name="job">Job that started this request</param>
        /// <param name="results">Results class for the Backtest job</param>
        /// <param name="endDate">The algorithms backtest end date</param>
        /// <param name="startDate">The algorithms backtest start date</param>
        /// <param name="progress">Progress of the packet. For the packet we assume progess of 100%.</param>
        public BacktestResultPacket(BacktestNodePacket job, BacktestResult results, DateTime endDate, DateTime startDate, decimal progress = 1m)
            : base(PacketType.BacktestResult)
        {
            try
            {
                Progress = Math.Round(progress, 3);
                SessionId = job.SessionId;
                PeriodFinish = endDate;
                PeriodStart = startDate;
                CompileId = job.CompileId;
                Channel = job.Channel;
                BacktestId = job.BacktestId;
                OptimizationId = job.OptimizationId;
                Results = results;
                Name = job.Name;
                UserId = job.UserId;
                ProjectId = job.ProjectId;
                SessionId = job.SessionId;
                TradeableDates = job.TradeableDates;
            }
            catch (Exception err)
            {
                Trace.TraceError(err.ToString());
                //Log.Error(err);
            }
        }

        /// <summary>
        /// Creates an empty result packet, useful when the algorithm fails to initialize
        /// </summary>
        /// <param name="job">The associated job packet</param>
        /// <returns>An empty result packet</returns>
        public static BacktestResultPacket CreateEmpty(BacktestNodePacket job)
        {
            return new BacktestResultPacket(job, new BacktestResult(new BacktestResultParameters(
                new Dictionary<string, Chart>(), new Dictionary<int, Order>(), new Dictionary<DateTime, decimal>(),
                new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, AlgorithmPerformance>(),
                new List<OrderEvent>(), new AlgorithmPerformance(),  new AlgorithmConfiguration()
            )), DateTime.UtcNow, DateTime.UtcNow);
        }
    } // End Queue Packet:


    /// <summary>
    /// Backtest results object class - result specific items from the packet.
    /// </summary>
    public class BacktestResult : QuantConnect.Result
    {
        /// <summary>
        /// Rolling window detailed statistics.
        /// </summary>
        public Dictionary<string, AlgorithmPerformance> RollingWindow = new Dictionary<string, AlgorithmPerformance>();

        /// <summary>
        /// Rolling window detailed statistics.
        /// </summary>
        public AlgorithmPerformance TotalPerformance = null;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public BacktestResult()
        {

        }

        /// <summary>
        /// Constructor for the result class using dictionary objects.
        /// </summary>
        public BacktestResult(BacktestResultParameters parameters)
        {
            throw new NotImplementedException();

            /*
            Charts = parameters.Charts;
            Orders = parameters.Orders;
            ProfitLoss = parameters.ProfitLoss;
            Statistics = parameters.Statistics;
            RuntimeStatistics = parameters.RuntimeStatistics;
            RollingWindow = parameters.RollingWindow;
            OrderEvents = parameters.OrderEvents;
            TotalPerformance = parameters.TotalPerformance;
            AlphaRuntimeStatistics = parameters.AlphaRuntimeStatistics;
            */
        }
    }
}
