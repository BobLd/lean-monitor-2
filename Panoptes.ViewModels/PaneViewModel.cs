using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Messages;
using Panoptes.Model.Settings;
using System.Threading.Tasks;

namespace Panoptes.ViewModels
{
    public abstract class DocumentPaneViewModel : PaneViewModel
    {
        private bool _canClose;
        private string _key;

        public DocumentPaneViewModel(IMessenger messenger, ISettingsManager settingsManager, ILogger<DocumentPaneViewModel> logger)
            : base(messenger, settingsManager, logger)
        { }

        public bool CanClose
        {
            get { return _canClose; }
            set
            {
                if (_canClose == value) return;
                _canClose = value;
                OnPropertyChanged();
            }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Key
        {
            get { return _key; }
            set
            {
                _key = value;
                OnPropertyChanged();
            }
        }
    }

    public abstract class ToolPaneViewModel : PaneViewModel
    {
        public ToolPaneViewModel(IMessenger messenger, ISettingsManager settingsManager, ILogger<ToolPaneViewModel> logger)
            : base(messenger, settingsManager, logger)
        { }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// View model for a docking pane
    /// </summary>
    public abstract class PaneViewModel : ObservableRecipient
    {
        public ISettingsManager SettingsManager { get; }

        public ILogger Logger { get; }

        public PaneViewModel(IMessenger messenger, ISettingsManager settingsManager, ILogger<PaneViewModel> logger)
            : base(messenger)
        {
            Logger = logger;
            SettingsManager = settingsManager;
            Messenger.Register<PaneViewModel, SettingsMessage>(this, async (r, m) => await r.UpdateSettingsAsync(m.Value, m.Type).ConfigureAwait(false));
        }

        protected abstract Task UpdateSettingsAsync(UserSettings userSettings, UserSettingsUpdate type);

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value) return;

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive == value) return;
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }
}
