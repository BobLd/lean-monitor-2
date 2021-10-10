using Avalonia.Data.Converters;
using QuantConnect;
using System;
using System.Globalization;

namespace Panoptes.Views.Converters
{
    internal sealed class SymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Symbol symbol)
            {
                return symbol.ToString();
            }
            else if (value is SecurityIdentifier si)
            {
                return si.ToString();
            }

#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
            throw new NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
            throw new NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
        }
    }
}
