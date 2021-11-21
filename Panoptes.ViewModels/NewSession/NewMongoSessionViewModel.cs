using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model.MongoDB.Sessions;
using Panoptes.Model.Sessions;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.NewSession
{
    public sealed class NewMongoSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ISessionService _sessionService;

        public string Password { private get; set; }

        public NewMongoSessionViewModel(ISessionService sessionService)
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

                    // Need to open async
                    await _sessionService.OpenAsync(new MongoSessionParameters
                    {
                        CloseAfterCompleted = true,
                        Host = Host,
                        Port = int.Parse(Port),
                        UserName = UserName,
                        Password = secureString
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ocEx)
                {
                    Debug.WriteLine($"NewMongoSessionViewModel.OpenAsync: Operation was canceled.\n{ocEx}");
                    //Error = ocEx.ToString();
                }
                catch (TimeoutException toEx)
                {
                    Error = toEx.ToString();
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
            };

            return fieldsToValidate.All(field => string.IsNullOrEmpty(this[field]));
        }

        private string _host = "localhost";
        public string Host
        {
            get { return _host; }
            set
            {
                _host = value;
                OnPropertyChanged();
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

        private string _port = "27017";
        public string Port
        {
            get { return _port; }
            set
            {
                _port = value;
                OnPropertyChanged();
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

#if DEBUG
        private string _username = "admin-bob";
#else
        private string _username = "";
#endif

        public string UserName
        {
            get { return _username; }
            set
            {
                _username = value;
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

                        //TODO password and username
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
