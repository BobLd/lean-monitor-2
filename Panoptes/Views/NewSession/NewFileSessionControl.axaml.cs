using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Toolkit.Mvvm.Input;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Views.NewSession
{
    public partial class NewFileSessionControl : UserControl
    {
        private readonly TextBox _textBoxFileName;
        public NewFileSessionControl()
        {
            InitializeComponent();
            _textBoxFileName = this.Get<TextBox>("_textBoxFileName");
            this.Get<Button>("_buttonOpenFile").Command = new AsyncRelayCommand(GetPath);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public async Task GetPath(CancellationToken cancellationToken)
        {
            try
            {
                var dialog = new OpenFileDialog();
                // set directory to previous one used
                dialog.Filters.Add(new FileDialogFilter() { Name = "Result", Extensions = { "json", "qcb" } }); // extension to do - compressed backtest files
                dialog.AllowMultiple = false;

                if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var result = await dialog.ShowAsync(desktop.MainWindow).ConfigureAwait(false);
                    if (result?.Length > 0)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => _textBoxFileName.Text = result[0]).ConfigureAwait(false);
                    }
                    return;
                }

                // Other type of ApplicationLifetime
                throw new ArgumentException($"Unknown ApplicationLifetime type, got '{App.Current.ApplicationLifetime?.GetType()}'.");
            }
            catch (TypeLoadException tlEx)
            {
                // TypeLoadException when publishing in signle file - apparently fixed in 
                // https://github.com/AvaloniaUI/Avalonia/pull/7028
                Log.Error(tlEx, "NewFileSessionControl.GetPath: Known Avalonia error when publishing in signle file, should be fixed in https://github.com/AvaloniaUI/Avalonia/pull/7028");
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "NewFileSessionControl.GetPath");
                throw;
            }
        }
    }
}
