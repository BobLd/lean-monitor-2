using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Panoptes.Avalonia.Views.Panels
{
    public partial class TradesPanel : UserControl
    {
        public TradesPanel()
        {
            InitializeComponent();

            // We do PropertyChanged here because the xaml thros some strange errors

            var from = this.Get<CalendarDatePicker>("_calendarDatePickerFrom");
            from.AddHandler(TextInputEvent, cdpTextInput, RoutingStrategies.Tunnel);
            from.PropertyChanged += _calendarDatePicker_PropertyChanged;

            var to = this.Get<CalendarDatePicker>("_calendarDatePickerTo");
            to.AddHandler(TextInputEvent, cdpTextInput, RoutingStrategies.Tunnel);
            to.PropertyChanged += _calendarDatePicker_PropertyChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void _calendarDatePicker_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != CalendarDatePicker.TextProperty || e.NewValue?.ToString()?.Length != 10)
            {
                return;
            }

            if (sender is not CalendarDatePicker cdp)
            {
                return;
            }

            var tb = GetCalendarDatePickerTextBox(cdp);
            if (tb == null) return;

            tb.RaiseEvent(new KeyEventArgs()
            {
                RoutedEvent = KeyDownEvent,
                Handled = false,
                Key = Key.Enter,
                KeyModifiers = KeyModifiers.None,
                Source = tb
            });
        }

        private void cdpTextInput(object? sender, TextInputEventArgs e)
        {
            if (sender is not CalendarDatePicker cdp)
            {
                return;
            }

            var tb = GetCalendarDatePickerTextBox(cdp);
            if (tb == null) return;

            if (tb.CaretIndex > 9)
            {
                e.Handled = true;
            }
        }

        private static TextBox? GetCalendarDatePickerTextBox(CalendarDatePicker cdp)
        {
            var tb = cdp.FindDescendantOfType<TextBox>();
            if (tb == null || tb.Name != "PART_TextBox")
            {
                return null;
            }

            return tb;
        }
    }
}
