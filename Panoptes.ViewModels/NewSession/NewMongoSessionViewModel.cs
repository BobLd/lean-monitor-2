using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model.MongoDB.Sessions;
using Panoptes.Model.Sessions;
using System.ComponentModel;
using System.Linq;
using System.Security;

namespace Panoptes.ViewModels.NewSession
{
    public class NewMongoSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ISessionService _sessionService;

        public string Password { private get; set; }

        public NewMongoSessionViewModel(ISessionService sessionService)
        {
            _sessionService = sessionService;
            OpenCommand = new RelayCommand(Open, CanOpen);
        }

        private void Open()
        {
            var secureString = new SecureString();
            foreach (var c in Password)
            {
                secureString.AppendChar(c);
            }
            Password = string.Empty;

            _sessionService.Open(new MongoSessionParameters
            {
                CloseAfterCompleted = true,
                Host = Host,
                Port = int.Parse(Port),
                UserName = UserName,
                Password = secureString
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

        private string _port = "27017";
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

        private string _username = "admin-bob";
        public string UserName
        {
            get { return _username; }
            set
            {
                _username = value;
                OnPropertyChanged();
                OpenCommand.NotifyCanExecuteChanged();
            }
        }

        public RelayCommand OpenCommand { get; }

        public string Header { get; } = "From MongoDB";

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

                        //TODO password and usernaem
                }

                return string.Empty;
            }
        }

        public string Error { get; }
    }
}
