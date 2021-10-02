using Avalonia.Data.Converters;
using OxyPlot.Axes;
using System;
using System.Globalization;

namespace Panoptes.Avalonia.Views.Charts
{
    internal sealed class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "NULL";
            }

            if (value is double date)
            {
                return DateTimeAxis.ToDateTime(date).ToString();
            }
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
            throw new NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return DateTimeAxis.ToDouble(date);
            }
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
            throw new NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
        }
    }
}
