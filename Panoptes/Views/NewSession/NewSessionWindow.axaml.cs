using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;

namespace Panoptes.Views.NewSession
{
    public partial class NewSessionWindow : Window
    {
        private readonly IMessenger _messenger;

        public NewSessionWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            // TODO: Implement dependency injection for the messenger
            _messenger = (WeakReferenceMessenger)App.Current.Services.GetService(typeof(IMessenger));
            _messenger.Register<SessionOpenedMessage>(this, (_, _) => Close());
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
