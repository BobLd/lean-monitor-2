using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;

namespace Panoptes.Views.Windows
{
    public partial class SplashScreenWindow : Window
    {
        private readonly IMessenger _messenger;

        private readonly Label _loadingLabel;

        public SplashScreenWindow()
        {
            _messenger = (IMessenger)App.Current.Services.GetService(typeof(IMessenger));
            if (_messenger == null)
            {
                throw new ArgumentNullException("Could not find 'IMessenger' service in 'App.Current.Services'.");
            }

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _loadingLabel = this.Get<Label>("_loadingLabel");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
