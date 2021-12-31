using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.Model.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Panoptes.ViewModels.Panels
{
    public sealed class LogPanelViewModel : ToolPaneViewModel
    {
        private enum ActionsThreadUI : byte
        {
            /// <summary>
            /// Add log entry.
            /// </summary>
            LogEntryAdd = 0,

            /// <summary>
            /// Clear observable collections.
            /// </summary>
            Clear = 1,
        }

        private readonly BackgroundWorker _resultBgWorker;

        private readonly BlockingCollection<LogEntryReceivedMessage> _resultsQueue = new BlockingCollection<LogEntryReceivedMessage>();

        private ObservableCollection<LogPanelItemViewModel> _logEntries = new ObservableCollection<LogPanelItemViewModel>();
        public ObservableCollection<LogPanelItemViewModel> LogEntries
        {
            get { return _logEntries; }
            set
            {
                _logEntries = value;
                OnPropertyChanged();
            }
        }

        public LogPanelViewModel(IMessenger messenger, ISettingsManager settingsManager, ILogger<LogPanelViewModel> logger)
            : base(messenger, settingsManager, logger)
        {
            Name = "Log";
            Messenger.Register<LogPanelViewModel, LogEntryReceivedMessage>(this, (r, m) => r._resultsQueue.Add(m));
            Messenger.Register<LogPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());

            _resultBgWorker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _resultBgWorker.DoWork += ResultQueueReader;
            _resultBgWorker.ProgressChanged += (s, e) =>
            {
                switch ((ActionsThreadUI)e.ProgressPercentage)
                {
                    case ActionsThreadUI.LogEntryAdd:
                        if (e.UserState is not LogPanelItemViewModel lpivm)
                        {
                            throw new ArgumentException($"LogPanelViewModel: Expecting {nameof(e.UserState)} of type 'LogPanelItemViewModel' but received '{e.UserState.GetType()}'", nameof(e));
                        }
                        LogEntries.Add(lpivm);
                        break;

                    case ActionsThreadUI.Clear:
                        LogEntries.Clear();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(e), "LogPanelViewModel: Unknown 'ProgressPercentage' passed.");
                }
            };

            _resultBgWorker.RunWorkerAsync();
        }

        protected override Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type)
        {
            Logger.LogDebug("LogPanelViewModel.UpdateSettingsAsync: {type}", type);
            return Task.CompletedTask;
        }

        private void Clear()
        {
            try
            {
                Logger.LogInformation("LogPanelViewModel: Clear");
                _resultBgWorker.ReportProgress((int)ActionsThreadUI.Clear);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LogPanelViewModel");
                throw;
            }
        }

        private void ResultQueueReader(object sender, DoWorkEventArgs e)
        {
            while (!_resultBgWorker.CancellationPending)
            {
                var logEntryMessage = _resultsQueue.Take(); // Need cancelation token
                _resultBgWorker.ReportProgress((int)ActionsThreadUI.LogEntryAdd, new LogPanelItemViewModel
                {
                    DateTime = logEntryMessage.DateTime,
                    Message = logEntryMessage.Message,
                    EntryType = logEntryMessage.EntryType
                });
            }
        }
    }
}
