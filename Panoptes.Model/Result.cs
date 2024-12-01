using Panoptes.Model.Charting;
using QuantConnect;
using QuantConnect.Orders;
using QuantConnect.Statistics;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace Panoptes.Model
{
    public class Result
    {
        public ResultType ResultType { get; set; }
        public IDictionary<string, ChartDefinition> Charts { get; set; }
        public IDictionary<int, Order> Orders { get; set; }
        public IDictionary<DateTime, decimal> ProfitLoss { get; set; }
        public IDictionary<string, string> Statistics { get; set; }
        public IDictionary<string, string> RuntimeStatistics { get; set; }
        public IDictionary<string, AlgorithmPerformance> RollingWindow { get; set; }
        public List<OrderEvent> OrderEvents { get; set; }
        public IDictionary<string, Holding> Holdings { get; set; }
        public CashBook Cash { get; set; }
        public string AccountCurrency { get; set; }
        public string AccountCurrencySymbol { get; set; }
        public IDictionary<string, string> ServerStatistics { get; set; }
    }
}