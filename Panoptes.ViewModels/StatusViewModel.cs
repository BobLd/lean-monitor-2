using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using Panoptes.Model.Sessions;
using QuantConnect;

namespace Panoptes.ViewModels
{
    public sealed class StatusViewModel : ObservableRecipient
    {
        private readonly IMessenger _messenger;
        private readonly ISessionService _sessionService;

        public StatusViewModel(IMessenger messenger, ISessionService sessionService) : base(messenger)
        {
            _messenger = messenger;
            _sessionService = sessionService;

            _messenger.Register<StatusViewModel, SessionOpenedMessage>(this, (r, _) =>
            {
                r.Progress = 0;
                r.IsProgressIndeterminate = false;
                r.OnPropertyChanged(nameof(IsSessionActive));
            });

            _messenger.Register<StatusViewModel, SessionClosedMessage>(this, (r, _) =>
            {
                r.SessionName = string.Empty;
                r.ProjectName = string.Empty;
                r.SessionState = SessionState.Unsubscribed;
                r.OnPropertyChanged(nameof(IsSessionActive));
            });

            _messenger.Register<StatusViewModel, SessionStateChangedMessage>(this, (r, m) => r.SessionState = m.State);

            _messenger.Register<StatusViewModel, SessionUpdateMessage>(this, (r, m) =>
            {
                r.Progress = m.ResultContext.Progress;
                r.SessionName = m.ResultContext.Name; // this updates the status bar...
                r.ProjectName = m.ResultContext.Project;

                switch (m.ResultContext.Result.ResultType)
                {
                    case ResultType.Backtest:
                        r.IsProgressIndeterminate = false;
                        return;

                    case ResultType.Live:
                        r.IsProgressIndeterminate = true;
                        return;
                }
            });

            _messenger.Register<StatusViewModel, AlgorithmStatusMessage>(this, (r, m) => r.AlgorithmStatus = m.Value.Status);
        }

        private decimal _progress;
        public decimal Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        private string _sessionName;
        public string SessionName
        {
            get { return _sessionName; }
            set
            {
                if (_sessionName == value) return;
                _sessionName = value;
                OnPropertyChanged();
            }
        }

        private string _projectName;
        public string ProjectName
        {
            get { return _projectName; }
            set
            {
                if (_projectName == value) return;
                _projectName = value;
                OnPropertyChanged();
            }
        }

        private bool _isProgressIndeterminate;
        public bool IsProgressIndeterminate
        {
            get { return _isProgressIndeterminate; }
            set
            {
                if (_isProgressIndeterminate == value) return;
                _isProgressIndeterminate = value;
                OnPropertyChanged();
            }
        }

        private SessionState _sessionState = SessionState.Unsubscribed;
        public SessionState SessionState
        {
            get { return _sessionState; }
            set
            {
                _sessionState = value;
                OnPropertyChanged();
            }
        }

        private AlgorithmStatus? _algorithmStatus;
        public AlgorithmStatus? AlgorithmStatus
        {
            get { return _algorithmStatus; }
            set
            {
                if (_algorithmStatus == value) return;
                _algorithmStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsSessionActive => _sessionService.IsSessionActive;
    }
}
