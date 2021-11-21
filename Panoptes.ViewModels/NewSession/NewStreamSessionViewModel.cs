using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.Stream;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.NewSession
{
    public sealed class NewStreamSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ISessionService _sessionService;

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

        private Task OpenAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                _sessionService.OpenAsync(new StreamSessionParameters
                {
                    CloseAfterCompleted = true,
                    Host = Host,
                    Port = int.Parse(Port)
                }, cancellationToken);
            }, cancellationToken);
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

        private string _port = "33333";
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

        public string Error { get; }
    }
}
