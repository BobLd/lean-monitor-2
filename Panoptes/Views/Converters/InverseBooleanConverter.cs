using Avalonia.Data.Converters;
using System;

namespace Panoptes.Views.Converters
{
    // https://stackoverflow.com/questions/1039636/how-to-bind-inverse-boolean-properties-in-wpf
    internal sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
