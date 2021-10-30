using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.File;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.NewSession
{
    public class NewFileSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ISessionService _sessionService;
        private readonly FileSessionParameters _fileSessionParameters = new FileSessionParameters
        {
#if DEBUG
            FileName = @"", // to change
#else
            FileName = "",
#endif
            Watch = true
        };

        public NewFileSessionViewModel(ISessionService sessionService)
        {
            _sessionService = sessionService;
            OpenCommandAsync = new AsyncRelayCommand(OpenAsync, CanOpen);
        }

        private Task OpenAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
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
            }, cancellationToken);
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

        public string Header { get; } = "File";

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
