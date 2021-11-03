using Microsoft.Toolkit.Mvvm.ComponentModel;
using Panoptes.Model.Statistics;

namespace Panoptes.ViewModels.Panels
{
    public sealed class StatisticViewModel : ObservableObject
    {
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

        private string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        private string _definition;
        public string Definition
        {
            get { return _definition; }
            set
            {
                if (_definition == value) return;
                _definition = value;
                OnPropertyChanged();
            }
        }

        private StatisticState _state = StatisticState.Inconclusive;
        public StatisticState State
        {
            get { return _state; }
            set
            {
                if (_state == value) return;
                _state = value;
                OnPropertyChanged();
            }
        }
    }
}
