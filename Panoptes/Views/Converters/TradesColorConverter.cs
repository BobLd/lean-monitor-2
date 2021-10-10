using Avalonia.Data.Converters;
using Avalonia.Media;
using QuantConnect.Orders;
using System;
using System.Globalization;

namespace Panoptes.Views.Converters
{
    internal sealed class TradesColorConverter : IValueConverter
    {
        private readonly IBrush _defaultBrush;

        public TradesColorConverter()
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
        }

        //https://github.com/AvaloniaUI/Avalonia/issues/2819
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrderStatus status)
            {
                switch (status)
                {
                    case OrderStatus.Invalid:
                        return Brushes.Red; //Brush.Parse("#E53D00");

                    case OrderStatus.Canceled:
                    case OrderStatus.CancelPending:
                        return Brushes.OrangeRed; // Brush.Parse("#FA824C");

                    case OrderStatus.Submitted:
                    case OrderStatus.UpdateSubmitted:
                        return Brushes.LightBlue;

                    case OrderStatus.PartiallyFilled:
                    case OrderStatus.Filled:
                        return Brushes.LimeGreen; //.Parse("#0BD55C");

                    case OrderStatus.None:
                    case OrderStatus.New:
                        return _defaultBrush;
                }
            }
            else if (value is OrderDirection direction)
            {
                switch (direction)
                {
                    case OrderDirection.Buy:
                        return Brushes.LimeGreen;

                    case OrderDirection.Sell:
                        return Brushes.Red;

                    case OrderDirection.Hold:
                        return _defaultBrush;
                }
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
