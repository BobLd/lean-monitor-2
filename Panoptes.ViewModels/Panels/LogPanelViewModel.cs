using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using System.Collections.ObjectModel;

namespace Panoptes.ViewModels.Panels
{
    public sealed class LogPanelViewModel : ToolPaneViewModel
    {
        private readonly IMessenger _messenger;

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

        public LogPanelViewModel()
        {
            Name = "Log";
        }

        public LogPanelViewModel(IMessenger messenger) : this()
        {
            _messenger = messenger;
            _messenger.Register<LogPanelViewModel, LogEntryReceivedMessage>(this, (r, m) => r.ParseResult(m));
            _messenger.Register<LogPanelViewModel, SessionClosedMessage>(this, (r, _) => r.Clear());
        }

        private void Clear()
        {
            LogEntries.Clear();
        }

        private void ParseResult(LogEntryReceivedMessage message)
        {
            // Need to use BackgroundWorker
            LogEntries.Add(new LogPanelItemViewModel
            {
                DateTime = message.DateTime,
                Message = message.Message,
                EntryType = message.EntryType
            });
        }
    }
}
