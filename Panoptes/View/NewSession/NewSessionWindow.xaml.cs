using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using System.Windows;

namespace Panoptes.View.NewSession
{
    /// <summary>
    /// Interaction logic for NewSessionWindow.xaml
    /// </summary>
    public partial class NewSessionWindow : Window
    {
        private readonly IMessenger _messenger;

        public NewSessionWindow()
        {
            InitializeComponent();

            // TODO: Implement dependency injection for the messenger
            _messenger = (WeakReferenceMessenger)App.Current.Services.GetService(typeof(IMessenger));
            _messenger.Register<SessionOpenedMessage>(this, (recipient, message) => Close());
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
