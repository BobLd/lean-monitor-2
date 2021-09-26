using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.File;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Panoptes.ViewModels.NewSession
{
    public class NewFileSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ISessionService _sessionService;
        private readonly FileSessionParameters _fileSessionParameters = new FileSessionParameters
        {
            FileName = "",
            Watch = true
        };

        public NewFileSessionViewModel(ISessionService sessionService)
        {
            _sessionService = sessionService;
            OpenCommand = new RelayCommand(Open, CanOpen);
        }

        private void Open()
        {
            try
            {
                // TODO: start waiting icon
                _sessionService.Open(_fileSessionParameters);
            }
            catch (System.Exception)
            {
                throw;
            }
            finally
            {
                // 
            }
        }

        private bool CanOpen()
        {
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
                OnPropertyChanged();
                OpenCommand.NotifyCanExecuteChanged();
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

        public RelayCommand OpenCommand { get; }

        public string Header { get; } = "From file";

        public string Error { get; }

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
