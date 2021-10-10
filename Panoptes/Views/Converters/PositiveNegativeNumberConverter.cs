using Avalonia.Data.Converters;
using Avalonia.Media;
using Panoptes.Model.Statistics;
using System;
using System.Globalization;

namespace Panoptes.Views.Converters
{
    internal sealed class PositiveNegativeNumberConverter : IValueConverter
    {
        private readonly IBrush _positiveBrush;
        private readonly IBrush _negativeBrush;
        private readonly IBrush _defaultBrush;

        public PositiveNegativeNumberConverter()
        {
            if (App.Current.Styles.TryGetResource("ThemeForegroundBrush", out var b) && b != null)
            {
                _defaultBrush = (IBrush)b;
            }
            else
            {
                // Not found
                _defaultBrush = Brushes.BurlyWood;
            }

            _positiveBrush = Brushes.LimeGreen;
            _negativeBrush = Brushes.Red;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d)
            {
                if (d > 0)
                {
                    return _positiveBrush;
                }
                else if (d < 0)
                {
                    return _negativeBrush;
                }
                return _defaultBrush;
            }
            else if (value is int i)
            {
                if (i > 0)
                {
                    return _positiveBrush;
                }
                else if (i < 0)
                {
                    return _negativeBrush;
                }
                return _defaultBrush;
            }
            else if (value is StatisticState statisticState)
            {
                if (statisticState == StatisticState.Positive)
                {
                    return _positiveBrush;
                }
                else if (statisticState == StatisticState.Negative)
                {
                    return _negativeBrush;
                }
                return _defaultBrush;
            }

            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
