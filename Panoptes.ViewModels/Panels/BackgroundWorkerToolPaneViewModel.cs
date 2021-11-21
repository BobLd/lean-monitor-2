using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using System;
using System.ComponentModel;

namespace Panoptes.ViewModels.Panels
{
    public class BackgroundWorkerToolPaneViewModel : ToolPaneViewModel
    {
        protected readonly BackgroundWorker _backgroundWorker;

        public BackgroundWorkerToolPaneViewModel(IMessenger messenger, string name)
            : base(messenger)
        {
            Name = name;
            _backgroundWorker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _backgroundWorker.DoWork += DoWork;
            _backgroundWorker.ProgressChanged += ProgressChanged;

            _backgroundWorker.RunWorkerCompleted += (s, e) => { /*do anything here*/ };
            _backgroundWorker.RunWorkerAsync();
        }

        private bool _displayLoading;
        public bool DisplayLoading
        {
            get
            {
                return _displayLoading;
            }

            set
            {
                if (_displayLoading == value) return;
                _displayLoading = value;
                OnPropertyChanged();
            }
        }

        protected virtual void DoWork(object sender, DoWorkEventArgs e)
        {
            throw new NotImplementedException("'DoWork' needs to be overridden");
        }

        protected virtual void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException("'ProgressChanged' needs to be overridden");
        }

        protected virtual void Clear()
        {
            throw new NotImplementedException("'Clear' needs to be overridden");
        }

        protected virtual void ProcessNewDay(TimerMessage.TimerEventType timerEventType)
        {
            // Doing nothing here
        }
    }
}
