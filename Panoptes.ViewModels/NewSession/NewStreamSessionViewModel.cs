using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.Stream;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.NewSession
{
    public sealed class NewStreamSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ISessionService _sessionService;
        private readonly StreamSessionParameters _sessionParameters = new StreamSessionParameters
        {
            Host = "localhost",
            Port = "33333"
        };

        public NewStreamSessionViewModel(ISessionService sessionService)
        {
            _sessionService = sessionService;

            OpenCommandAsync = new AsyncRelayCommand(OpenAsync, CanOpen);

            CancelCommand = new RelayCommand(() =>
            {
                if (OpenCommandAsync.CanBeCanceled)
                {
                    OpenCommandAsync.Cancel();
                }
            });
        }

        private async Task OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _sessionService.OpenAsync(_sessionParameters, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ocEx)
            {
                Debug.WriteLine($"NewStreamSessionViewModel.OpenAsync: Operation was canceled.\n{ocEx}");
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    Error = ex.InnerException.ToString();
                }
                else
                {
                    Error = ex.ToString();
                }
            }
        }

        private bool CanOpen()
        {
            var fieldsToValidate = new[]
            {
                nameof(Host),
                nameof(Port),
            };

            return fieldsToValidate.All(field => string.IsNullOrEmpty(this[field]));
        }

        public void LoadParameters(IDictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0) return;

            if (parameters.TryGetValue(nameof(Host), out var host))
            {
                Host = host;
            }

            if (parameters.TryGetValue(nameof(Port), out var port))
            {
                Port = port;
            }
        }

        public string Host
        {
            get { return _sessionParameters.Host; }
            set
            {
                if (_sessionParameters.Host == value) return;
                _sessionParameters.Host = value;
                OnPropertyChanged();
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

        public string Port
        {
            get { return _sessionParameters.Port.ToString(); }
            set
            {
                if (_sessionParameters.Port == value) return;
                _sessionParameters.Port = value;
                OnPropertyChanged();
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

        public AsyncRelayCommand OpenCommandAsync { get; }

        public RelayCommand CancelCommand { get; }

        public string Header { get; } = "Stream";

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Host):
                        if (string.IsNullOrWhiteSpace(Host)) return "Host is required";
                        break;

                    case nameof(Port):
                        if (string.IsNullOrWhiteSpace(Port)) return "Port is required";
                        if (!int.TryParse(Port, out _)) return "Port should be numeric";
                        break;
                }

                return string.Empty;
            }
        }

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
    }
}
