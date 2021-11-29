using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Settings;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Panoptes.ViewModels
{
    public sealed class SettingsViewModel : ObservableRecipient
    {
        public readonly ISettingsManager SettingsManager;

        public AsyncRelayCommand SaveCommandAsync { get; }

        private bool _hasChanged;

        public SettingsViewModel(IMessenger messenger, ISettingsManager settingsManager) : base(messenger)
        {
            SettingsManager = settingsManager;

            TimeZones = new ObservableCollection<TimeZoneInfo>(TimeZoneInfo.GetSystemTimeZones());
            Grids = new ObservableCollection<string>(SettingsManager.SetupGridsColumns);

            SaveCommandAsync = new AsyncRelayCommand(SaveChanges, () => _hasChanged);

            //Messenger.Register<SettingsViewModel, ShowNewSessionWindowMessage>(this, async (r, _) => await r._settingsManager.InitialiseAsync().ConfigureAwait(false));
            //Messenger.Register<SettingsViewModel, SessionOpenedMessage>(this, async (r, _) => await r._settingsManager.InitialiseAsync().ConfigureAwait(false));
        }

        public ObservableCollection<TimeZoneInfo> TimeZones { get; }

        public ObservableCollection<string> Grids { get; }

        public bool SoundsActivated
        {
            get
            {
                return SettingsManager.SoundsActivated;
            }

            set
            {
                SettingsManager.SetSoundsActivated(value);
                OnPropertyChanged();
                NotifyChanged();
            }
        }

        private string _selectedGrid;
        public string SelectedGrid
        {
            get { return _selectedGrid; }
            set
            {
                if (_selectedGrid == value) return;
                _selectedGrid = value;
                OnPropertyChanged();
            }
        }

        public string SelectedTimeZoneId => SelectedTimeZone.Id;

        public TimeZoneInfo SelectedTimeZone
        {
            get
            {
                return SettingsManager.SelectedTimeZone;
            }

            set
            {
                if (SettingsManager.SelectedTimeZone == value) return;
                SettingsManager.SelectedTimeZone = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedTimeZoneId));
                NotifyChanged();
            }
        }

        private void NotifyChanged()
        {
            _hasChanged = true;
            SaveCommandAsync.NotifyCanExecuteChanged();
        }

        private async Task SaveChanges()
        {
            try
            {
                await SettingsManager.SaveAsync().ConfigureAwait(false);
                _hasChanged = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsViewModel.SaveChanges: Something wrong happened.\n{ex}");
                _hasChanged = true;
            }
            finally
            {
                SaveCommandAsync.NotifyCanExecuteChanged();
            }
        }
    }
}
