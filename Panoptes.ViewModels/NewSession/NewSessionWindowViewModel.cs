using Microsoft.Toolkit.Mvvm.ComponentModel;
using Panoptes.Model.Settings;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Panoptes.ViewModels.NewSession
{
    public sealed class NewSessionWindowViewModel : ObservableRecipient
    {
        private ObservableCollection<INewSessionViewModel> _newSessionViewModels;
        private INewSessionViewModel _selectedViewModel;

        public ObservableCollection<INewSessionViewModel> NewSessionViewModels
        {
            get
            {
                return _newSessionViewModels;
            }

            set
            {
                _newSessionViewModels = value;
                OnPropertyChanged();
            }
        }

        public INewSessionViewModel SelectedViewModel
        {
            get
            {
                return _selectedViewModel;
            }

            set
            {
                _selectedViewModel = value;
                OnPropertyChanged();
            }
        }

        public NewSessionWindowViewModel(IEnumerable<INewSessionViewModel> newSessionViewModels, ISettingsManager settingsManager)
        {
            string type = null;
            if (settingsManager.SessionParameters != null)
            {
                type = $"New{settingsManager.SessionParameters["type"].Replace("Parameters", "")}ViewModel";
            }

            foreach (var sessionViewModel in newSessionViewModels)
            {
                sessionViewModel.OpenCommandAsync.PropertyChanged += OpenCommandAsync_PropertyChanged;
                if (!string.IsNullOrEmpty(type) && sessionViewModel.GetType().Name == type)
                {
                    SelectedViewModel = sessionViewModel;
                    SelectedViewModel.LoadParameters(settingsManager.SessionParameters);
                }
            }
            NewSessionViewModels = new ObservableCollection<INewSessionViewModel>(newSessionViewModels);
        }

        private void OpenCommandAsync_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsAnyRunning = NewSessionViewModels.Any(ns => ns.OpenCommandAsync.IsRunning);
        }

        private bool _isAnyRunning;
        public bool IsAnyRunning
        {
            get { return _isAnyRunning; }

            set
            {
                if (_isAnyRunning == value) return;
                _isAnyRunning = value;
                OnPropertyChanged();
            }
        }
    }
}
