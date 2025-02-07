using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using QuantConnect;
using System;
using System.Globalization;

namespace Panoptes.Views.Converters
{
    internal sealed class StatusBarColorConverter : IValueConverter
    {
        private readonly IBrush _defaultBrush;

        public StatusBarColorConverter()
        {
            if (App.Current.Styles.TryGetResource("ThemeControlTransparentBrush", ThemeVariant.Dark, out var b) && b != null)
            {
                _defaultBrush = (IBrush)b;
            }
            else
            {
                // Not found
                _defaultBrush = Brushes.BurlyWood;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlgorithmStatus algorithmStatus)
            {
                switch (algorithmStatus)
                {
                    case AlgorithmStatus.LoggingIn:
                        return Brushes.CornflowerBlue;

                    case AlgorithmStatus.InQueue:
                        return Brushes.DeepSkyBlue;

                    case AlgorithmStatus.Initializing:
                        return Brushes.DodgerBlue;

                    case AlgorithmStatus.Running:
                        return Brushes.Green;

                    case AlgorithmStatus.Invalid:
                    case AlgorithmStatus.Stopped:
                        return Brushes.OrangeRed;

                    case AlgorithmStatus.DeployError:
                    case AlgorithmStatus.RuntimeError:
                        return Brushes.Red;

                    case AlgorithmStatus.Completed:
                        return Brushes.DarkSlateBlue;

                    case AlgorithmStatus.Deleted:
                    case AlgorithmStatus.History:
                    case AlgorithmStatus.Liquidated:
                        return Brushes.Tomato;

                    default:
                        return Brushes.PaleVioletRed;
                }
            }
            return _defaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
            throw new NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
        }
    }
}
