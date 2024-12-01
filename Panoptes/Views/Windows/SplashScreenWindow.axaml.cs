using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using System;

namespace Panoptes.Views.Windows
{
    public partial class SplashScreenWindow : Window
    {
        private readonly IMessenger _messenger;

        private readonly Label _statusLabel;
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
            _statusLabel = this.Get<Label>("_statusLabel");
            _statusLabel.Content = $"Opening {Global.AppName}...";

            _loadingLabel = this.Get<Label>("_loadingLabel");
            _loadingLabel.Content = $"v{Global.AppVersion}";

            this.Opened += SplashScreenWindow_Opened;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SplashScreenWindow_Opened(object? sender, EventArgs e)
        {
            CenterWindowOnPrimaryScreen();
        }

        private void CenterWindowOnPrimaryScreen()
        {
            var primaryScreen = Screens.Primary;

            if (primaryScreen == null)
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            var screenBounds = primaryScreen.Bounds;

            double windowWidth = this.Width;
            double windowHeight = this.Height;

            double left = screenBounds.X + (screenBounds.Width - windowWidth) / 2;
            double top = screenBounds.Y + (screenBounds.Height - windowHeight) / 2;

            this.Position = new PixelPoint(
                (int)Math.Round(left),
                (int)Math.Round(top));
        }
    }
}
