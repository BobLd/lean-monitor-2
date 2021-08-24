using OxyPlot.Axes;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Panoptes.View.Charts
{
    internal class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double date)
            {
                return DateTimeAxis.ToDateTime(date);
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return DateTimeAxis.ToDouble(date);
            }
            throw new NotImplementedException();
        }
    }
}
