﻿using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Panoptes.Model;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.File;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.NewSession
{
    public sealed class NewFileSessionViewModel : ObservableRecipient, INewSessionViewModel, IDataErrorInfo
    {
        private readonly ILogger _logger;
        private readonly ISessionService _sessionService;
        private readonly FileSessionParameters _sessionParameters = new FileSessionParameters
        {
#if DEBUG
            FileName = @"C:\Users\Bob\Desktop\bt\SableSMA_4_14\SableSMA_4_14.json",
#endif
            Watch = false
        };

        public NewFileSessionViewModel(ISessionService sessionService, ILogger<NewFileSessionViewModel> logger)
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
            try
            {
                Error = null;
                await _sessionService.OpenAsync(_sessionParameters, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ocEx)
            {
                _logger.LogInformation("NewFileSessionViewModel.OpenAsync: Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NewFileSessionViewModel.OpenAsync");
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

        public void LoadParameters(IDictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0) return;

            if (parameters.TryGetValue(nameof(FileName), out var fileName))
            {
                FileName = fileName;
            }

            if (parameters.TryGetValue(nameof(FileWatch), out var fileWatchStr) && bool.TryParse(fileWatchStr, out var fileWatch))
            {
                FileWatch = fileWatch;
            }
        }

        public string FileNameAndSize
        {
            get
            {
                if (!File.Exists(FileName))
                {
                    return $"{FileName} (N/A MB)";
                }
                return $"{FileName} ({Global.GetFileSize(FileName):0.#} MB)";
            }
        }

        public string FileName
        {
            get { return _sessionParameters.FileName; }
            set
            {
                if (_sessionParameters.FileName == value) return;
                _sessionParameters.FileName = value;
                Error = null;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileNameAndSize));
                OpenCommandAsync.NotifyCanExecuteChanged();
            }
        }

        public bool FileWatch
        {
            get { return _sessionParameters.Watch; }
            set
            {
                _sessionParameters.Watch = value;
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
