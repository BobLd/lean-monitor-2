using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model.MongoDB.Sessions;
using Panoptes.Model.Sessions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.NewSession
{
    public sealed class NewMongoSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ILogger _logger;
        private readonly ISessionService _sessionService;
        private readonly MongoSessionParameters _sessionParameters = new MongoSessionParameters
        {
            Host = "localhost",
            Port = "27017",
#if DEBUG
            UserName = "admin-bob",
            DatabaseName = "backtest-test",
            CollectionName = "bar-3"
#endif
        };

        public string Password { private get; set; }

        public NewMongoSessionViewModel(ISessionService sessionService, ILogger<NewMongoSessionViewModel> logger)
        {
            _logger = logger;
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
            await Task.Run(async () =>
            {
                try
                {
                    Error = null;
                    var secureString = new SecureString();
                    if (!string.IsNullOrEmpty(Password))
                    {
                        foreach (var c in Password)
                        {
                            secureString.AppendChar(c);
                        }
                        Password = string.Empty;
                    }
                    _sessionParameters.Password = secureString;

                    // Need to open async
                    await _sessionService.OpenAsync(_sessionParameters, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ocEx)
                {
                    _logger.LogInformation("NewMongoSessionViewModel.OpenAsync: Operation was canceled.");
                    //Error = ocEx.ToString();
                }
                catch (TimeoutException toEx)
                {
                    _logger.LogError(toEx, "NewMongoSessionViewModel.OpenAsync");
                    Error = toEx.Message;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "NewMongoSessionViewModel.OpenAsync");
                    if (ex.InnerException != null)
                    {
                        Error = ex.InnerException.ToString();
                    }
                    else
                    {
                        Error = ex.ToString();
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private bool CanOpen()
        {
            if (OpenCommandAsync.IsRunning)
            {
                return false;
            }

            var fieldsToValidate = new[]
            {
                nameof(Host),
                nameof(Port),
                nameof(UserName),
                nameof(DatabaseName),
                nameof(CollectionName),
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

            if (parameters.TryGetValue(nameof(UserName), out var user))
            {
                UserName = user;
            }

            if (parameters.TryGetValue(nameof(DatabaseName), out var db))
            {
                DatabaseName = db;
            }

            if (parameters.TryGetValue(nameof(CollectionName), out var collec))
            {
                CollectionName = collec;
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
            get { return _sessionParameters.Port; }
            set
            {
                if (_sessionParameters.Port == value) return;
                _sessionParameters.Port = value;
                OnPropertyChanged();
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

        public string UserName
        {
            get { return _sessionParameters.UserName; }
            set
            {
                if (_sessionParameters.UserName == value) return;
                _sessionParameters.UserName = value;
                OnPropertyChanged();
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

        public string DatabaseName
        {
            get { return _sessionParameters.DatabaseName; }
            set
            {
                if (_sessionParameters.DatabaseName == value) return;
                _sessionParameters.DatabaseName = value;
                OnPropertyChanged();
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

        public string CollectionName
        {
            get { return _sessionParameters.CollectionName; }
            set
            {
                if (_sessionParameters.CollectionName == value) return;
                _sessionParameters.CollectionName = value;
                OnPropertyChanged();
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

        public AsyncRelayCommand OpenCommandAsync { get; }

        public RelayCommand CancelCommand { get; }

        public string Header { get; } = "MongoDB";

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

                    case nameof(UserName):
                        if (string.IsNullOrWhiteSpace(UserName)) return "User name is required";
                        break;

                    case nameof(DatabaseName):
                        if (string.IsNullOrWhiteSpace(DatabaseName)) return "Database name is required";
                        break;

                    case nameof(CollectionName):
                        if (string.IsNullOrWhiteSpace(CollectionName)) return "Collection name is required";
                        break;

                        //TODO password?
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
