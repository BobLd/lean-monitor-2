﻿using QuantConnect.Orders;
using QuantConnect.Statistics;
using System;
using System.Collections.Generic;

namespace Panoptes.Model
{
    public class Result
    {
        public Dictionary<string, Charting.ChartDefinition> Charts = new Dictionary<string, Charting.ChartDefinition>();

        // Todo: make order, and AlgorithmPerformance indepentent of QC namespace
        public Dictionary<int, Order> Orders = new Dictionary<int, Order>();
        public Dictionary<DateTime, decimal> ProfitLoss = new Dictionary<DateTime, decimal>();
        public Dictionary<string, string> Statistics = new Dictionary<string, string>();
        public Dictionary<string, string> RuntimeStatistics = new Dictionary<string, string>();
        public Dictionary<string, string> ServerStatistics = new Dictionary<string, string>();
        public Dictionary<string, AlgorithmPerformance> RollingWindow = new Dictionary<string, AlgorithmPerformance>();

        public List<OrderEvent> OrderEvents = new List<OrderEvent>();

        public ResultType ResultType { get; set; }

        public void Add(Result result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            ResultUpdater.Merge(this, result);
        }
    }
}
