﻿namespace Panoptes.Model.Statistics
{
    public interface IStatisticsFormatter
    {
        StatisticState Format(string key, string value);
    }
}
