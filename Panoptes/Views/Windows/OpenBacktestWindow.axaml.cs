using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using System;

namespace Panoptes.Views.Windows
{
    public partial class OpenBacktestWindow : Window
    {
        private readonly IMessenger _messenger;

        private readonly Label _loadingLabel;

        public OpenBacktestWindow()
        {
            _messenger = (IMessenger)App.Current.Services.GetService(typeof(IMessenger));
            if (_messenger == null)
            {
                throw new ArgumentNullException("Could not find 'IMessenger' service in 'App.Current.Services'.");
            }

            _messenger.Register<OpenBacktestWindow, SessionOpenedMessage>(this, (r, m) =>
            {
                if (m.IsSuccess)
                {
                    Dispatcher.UIThread.InvokeAsync(() => r.Close()).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                else
                {
                    string error = $"Something went wrong when openning the backtest:\n{m.Error}";
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.Get<ProgressBar>("_mainProgressBar").IsIndeterminate = false;
                        this.Get<Label>("_statusLabel").Content = "Failed to open backtest.";
                        var errorLabel = this.Get<TextBox>("_errorTextBlock");
                        errorLabel.Text = error;
                        errorLabel.IsEnabled = true;
                        errorLabel.IsVisible = true;
                    });
                    // TODO: Re-center window?
                }
            });

            InitializeComponent();

            _loadingLabel = this.Get<Label>("_loadingLabel");

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public object LoadingContent
        {
            get
            {
                return _loadingLabel.Content;
            }

            set
            {
                if (_loadingLabel.Content == value) return;
                _loadingLabel.Content = value;
            }
        }
    }
}
