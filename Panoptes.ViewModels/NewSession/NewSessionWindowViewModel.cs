using Microsoft.Toolkit.Mvvm.ComponentModel;
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

        public NewSessionWindowViewModel(IEnumerable<INewSessionViewModel> newSessionViewModels)
        {
            foreach (var session in newSessionViewModels)
            {
                session.OpenCommandAsync.PropertyChanged += OpenCommandAsync_PropertyChanged;
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
