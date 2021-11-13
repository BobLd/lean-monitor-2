using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.ViewModels;
using Panoptes.Views.NewSession;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Panoptes
{
    public partial class MainWindow : Window
    {
        private readonly IMessenger _messenger;

        public MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

        public MainWindow()
        {
            _messenger = (WeakReferenceMessenger)App.Current.Services.GetService(typeof(IMessenger));
            if (_messenger == null)
            {
                throw new ArgumentNullException("Could not find 'IMessenger' service in 'App.Current.Services'.");
            }
            _messenger.Register<MainWindow, ShowNewSessionWindowMessage>(this, (r, _) => r.ShowWindowDialog<NewSessionWindow>());

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            try
            {
                ViewModel?.ShutdownSession();

                if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    foreach (var window in desktop.Windows)
                    {
                        if (window is MainWindow) continue;
                        window.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // Need to log
                Trace.WriteLine(ex);
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            // Do nothing for the moment
        }

        private void OnOpened(object sender, EventArgs e)
        {
            // Tell the viewModel we have loaded and we can process data
            ViewModel.Initialize();
        }

        private void ShowWindowDialog<T>() where T : Window
        {
            var window = Activator.CreateInstance<T>();
            window.ShowDialog(this);
        }

        private static void OpenLink(string link)
        {
            try
            {
                // https://github.com/dotnet/runtime/issues/28005
                // not sure if work on every platforms
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = $"/c start {link}"
                });
            }
            catch (Exception e)
            {
                // Need to log
                Trace.WriteLine(e);
            }
        }

        private void ShowAboutButton_OnClick(object sender, RoutedEventArgs e)
        {
            //ShowWindowDialog<AboutWindow>();
        }

        private void BrowseLeanGithubMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenLink("https://github.com/QuantConnect/Lean");
        }

        private void BrowseMonitorGithubOriginalMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenLink("https://github.com/mirthestam/lean-monitor");
        }

        private void BrowseMonitorGithubMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenLink("https://github.com/BobLd/lean-monitor-2");
        }

        private void BrowseChartingDocumentationMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenLink("https://www.quantconnect.com/docs#Charting");
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            /*
            var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (fileNames != null) ViewModel.HandleDroppedFileName(fileNames[0]);
            */
        }

        private void MainWindow_OnDragOver(object sender, DragEventArgs e)
        {
            var fileNames = e.Data.GetFileNames()?.ToArray();
            if (fileNames?.Length == 1)
            {
                // Drag drop validated.
                return;
            }

            /*
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (fileNames != null && fileNames.Length == 1)
                {
                    // Drag drop validated.
                    return;
                }
            }
            */

            // Drag drop invalidated.
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
        }
    }
}
