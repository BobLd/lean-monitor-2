using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Panoptes.ViewModels.NewSession
{
    public class NewSessionWindowViewModel : ObservableRecipient
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
            get { return _selectedViewModel; }
            set
            {
                _selectedViewModel = value;
                OnPropertyChanged();
            }
        }

        public NewSessionWindowViewModel(IEnumerable<INewSessionViewModel> newSessionViewModels)
        {
            NewSessionViewModels = new ObservableCollection<INewSessionViewModel>(newSessionViewModels);
        }
    }
}
