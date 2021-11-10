namespace Panoptes.Model.Statistics
{
    public sealed class StatisticsFormatter : IStatisticsFormatter
    {
        public StatisticState Format(string key, string value)
        {
            switch (key)
            {
                case "Unrealized":
                case "Net Profit":
                case "Return":
                case "Sharpe Ratio":
                case "Probabilistic Sharpe Ratio":
                    return FormatNegativePositive(value);

                case "Fees":
                    return StatisticState.Inconclusive;

                default:
                    return FormatOnlyNegative(value);
            }
        }

        private static StatisticState FormatNegativePositive(string value)
        {
            return IsNegative(value) ? StatisticState.Negative : StatisticState.Positive;
        }

        private static StatisticState FormatOnlyNegative(string value)
        {
            return IsNegative(value) ? StatisticState.Negative : StatisticState.Inconclusive;
        }

        private static bool IsNegative(string value)
        {
            return value.Contains("-");
        }
    }
}
