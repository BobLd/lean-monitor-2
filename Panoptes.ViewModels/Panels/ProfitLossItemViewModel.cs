using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;

namespace Panoptes.ViewModels.Panels
{
    public sealed class ProfitLossItemViewModel : ObservableObject
    {
        private DateTime _dateTime;
        public DateTime DateTime
        {
            get { return _dateTime; }
            set
            {
                if (_dateTime == value) return;
                _dateTime = value;
                OnPropertyChanged();
            }
        }

        private decimal _profit;
        public decimal Profit
        {
            get { return _profit; }
            set
            {
                if (_profit == value) return;
                _profit = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNegative));
            }
        }

        public bool IsNegative
        {
            get { return Profit < 0; }
        }
    }
}
