using Avalonia.Data.Converters;
using Avalonia.Media;
using Panoptes.Model;
using System;
using System.Globalization;

namespace Panoptes.Views.Converters
{
    internal sealed class LogPanelLogoTypeConverter : IValueConverter
    {
        private readonly DrawingGroup Log;
        private readonly DrawingGroup Debug;
        private readonly DrawingGroup Error;
        private readonly DrawingGroup Monitor;

        public LogPanelLogoTypeConverter()
        {
            if (App.Current.Styles.TryGetResource("TablerIcons.InfoCircle", out var log) && log != null)
            {
                Log = (DrawingGroup)log;
            }

            if (App.Current.Styles.TryGetResource("TablerIcons.Bug", out var debug) && debug != null)
            {
                Debug = (DrawingGroup)debug;
            }

            if (App.Current.Styles.TryGetResource("TablerIcons.CircleX", out var error) && error != null)
            {
                Error = (DrawingGroup)error;
            }

            if (App.Current.Styles.TryGetResource("TablerIcons.Activity", out var monitor) && monitor != null)
            {
                Monitor = (DrawingGroup)monitor;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogItemType logType)
            {
                switch (logType)
                {
                    case LogItemType.Log:
                    default:
                        return Log;

                    case LogItemType.Debug:
                        return Debug;

                    case LogItemType.Error:
                        return Error;

                    case LogItemType.Monitor:
                        return Monitor;
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
