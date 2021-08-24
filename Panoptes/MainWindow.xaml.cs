using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.View.NewSession;
using Panoptes.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;

namespace Panoptes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IMessenger _messenger;

        public MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();

            _messenger = (WeakReferenceMessenger)App.Current.Services.GetService(typeof(IMessenger));
            _messenger.Register<MainWindow, ShowNewSessionWindowMessage>(this, (r, _) => r.ShowWindowDialog<NewSessionWindow>());

            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
            {
                App.Current.Windows[intCounter].Close();
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Tell the viewModel we have loaded and we can process data
            ViewModel.Initialize();
        }

        private void ShowWindowDialog<T>() where T : Window
        {
            var window = Activator.CreateInstance<T>();
            window.Owner = this;
            window.ShowDialog();
        }

        private static void OpenLink(string link)
        {
            try
            {
                Process.Start(link);
            }
            catch (Exception e)
            {
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

        private void BrowseMonitorGithubMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenLink("https://github.com/mirthestam/lean-monitor");
        }

        private void BrowseChartingDocumentationMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenLink("https://www.quantconnect.com/docs#Charting");
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (fileNames != null) ViewModel.HandleDroppedFileName(fileNames[0]);
        }

        private void MainWindow_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (fileNames != null && fileNames.Length == 1)
                {
                    // Drag drop validated.
                    return;
                }
            }

            // Drag drop invalidated.
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
    }
}
