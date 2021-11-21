using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.File;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.NewSession
{
    public sealed class NewFileSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ISessionService _sessionService;
        private readonly FileSessionParameters _fileSessionParameters = new FileSessionParameters
        {
#if DEBUG
            FileName = @"C:\Users\Bob\Desktop\bt\SableSMA_4_14\SableSMA_4_14.json",
#else
            FileName = "",
#endif
            Watch = false
        };

        public NewFileSessionViewModel(ISessionService sessionService)
        {
            _sessionService = sessionService;
            OpenCommandAsync = new AsyncRelayCommand(OpenAsync, CanOpen);
            OpenCommandAsync.PropertyChanged += OpenCommandAsync_PropertyChanged;

            CancelCommand = new RelayCommand(() =>
            {
                if (OpenCommandAsync.CanBeCanceled)
                {
                    OpenCommandAsync.Cancel();
                }
            });
        }

        private void OpenCommandAsync_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OpenCommandAsync.NotifyCanExecuteChanged();
        }

        private async Task OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                Error = null;
                await _sessionService.OpenAsync(_fileSessionParameters, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
            }
        }

        private bool CanOpen()
        {
            if (OpenCommandAsync.IsRunning)
            {
                return false;
            }

            var fieldsToValidate = new[]
            {
                nameof(FileName),
            };

            return fieldsToValidate.All(field => string.IsNullOrEmpty(this[field]));
        }

        public string FileName
        {
            get { return _fileSessionParameters.FileName; }
            set
            {
                _fileSessionParameters.FileName = value;
                Error = null;
                OnPropertyChanged();
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

        public bool FileWatch
        {
            get { return _fileSessionParameters.Watch; }
            set
            {
                _fileSessionParameters.Watch = value;
                OnPropertyChanged();
            }
        }

        public AsyncRelayCommand OpenCommandAsync { get; }

        public RelayCommand CancelCommand { get; }

        public string Header { get; } = "File";

        private string _error;
        public string Error
        {
            get { return _error; }

            set
            {
                if (_error == value) return;
                _error = value;
                OnPropertyChanged();
            }
        }

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(FileName):
                        if (string.IsNullOrWhiteSpace(FileName)) return "Filename is required";
                        if (!File.Exists(FileName)) return "File does not exist";
                        return string.Empty;

                    default:
                        return string.Empty;
                }
            }
        }
    }
}
