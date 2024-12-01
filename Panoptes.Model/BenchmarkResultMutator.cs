using Panoptes.Model.Charting;
using QuantConnect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Panoptes.Model
{
    public sealed class BenchmarkResultMutator : IResultMutator
    {
        public void Mutate(Result result)
        {
            if (!result.Charts.ContainsKey("Benchmark")) return;
            var benchmarkChart = result.Charts["Benchmark"];

            if (!benchmarkChart.Series.ContainsKey("Benchmark")) return;
            var benchmarkSeries = benchmarkChart.Series["Benchmark"];

            if (!result.Charts.ContainsKey("Strategy Equity")) return;
            var equityChart = result.Charts["Strategy Equity"];

            if (!equityChart.Series.ContainsKey("Equity")) return;
            var equitySeries = equityChart.Series["Equity"];

            var benchmarkLastUpdated = DateTimeOffset.MinValue;
            SeriesDefinition relativeBenchmarkSeries;
            if (!equityChart.Series.ContainsKey("Relative Benchmark"))
            {
                relativeBenchmarkSeries = new SeriesDefinition
                {
                    SeriesType = SeriesType.Line,
                    Name = "Relative Benchmark"
                };
                equityChart.Series.Add("Relative Benchmark", relativeBenchmarkSeries);
            }
            else
            {
                relativeBenchmarkSeries = equityChart.Series["Relative Benchmark"];
                benchmarkLastUpdated = relativeBenchmarkSeries.Values.Last().X;
            }

            Update(relativeBenchmarkSeries, benchmarkSeries, equitySeries, benchmarkLastUpdated);
        }

        private static void Update(SeriesDefinition relativeBenchmarkSeries, SeriesDefinition benchmarkSeries, SeriesDefinition equitySeries, DateTimeOffset lastUpdate)
        {
            var newBenchmarkValues = benchmarkSeries.Values.Where(v => v.X > lastUpdate).ToList();

            if (newBenchmarkValues.Count == 0) return;

            var relValues = new List<InstantChartPoint>();

            var equityOpenValue = equitySeries.Values[0].Y;

            relValues.Add(new InstantChartPoint(newBenchmarkValues[0].X, equityOpenValue));

            for (var i = 1; i < newBenchmarkValues.Count; i++)
            {
                var x = newBenchmarkValues[i].X;

                decimal y;

                var curBenchmarkValue = newBenchmarkValues[i].Y;
                var prevBenchmarkValue = newBenchmarkValues[i - 1].Y;
                if (prevBenchmarkValue == 0 || curBenchmarkValue == 0)
                {
                    y = relValues[i - 1].Y;
                }
                else
                {
                    y = relValues[i - 1].Y * (curBenchmarkValue / prevBenchmarkValue);
                }
                relValues.Add(new InstantChartPoint(x, y));
            }

            relativeBenchmarkSeries.Values.AddRange(relValues);
        }
    }
}
