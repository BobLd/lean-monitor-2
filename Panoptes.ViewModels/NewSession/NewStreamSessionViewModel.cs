using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.Stream;
using System.ComponentModel;
using System.Linq;

namespace Panoptes.ViewModels.NewSession
{
    public class NewStreamSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ISessionService _sessionService;

        public NewStreamSessionViewModel(ISessionService sessionService)
        {
            _sessionService = sessionService;

            OpenCommand = new RelayCommand(Open, CanOpen);
        }

        private void Open()
        {
            _sessionService.Open(new StreamSessionParameters
            {
                CloseAfterCompleted = true,
                Host = Host,
                Port = int.Parse(Port)
            });
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
                OpenCommand.NotifyCanExecuteChanged();
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
                OpenCommand.NotifyCanExecuteChanged();
            }
        }

        public RelayCommand OpenCommand { get; }

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
