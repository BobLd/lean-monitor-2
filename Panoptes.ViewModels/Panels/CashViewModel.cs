using Microsoft.Toolkit.Mvvm.ComponentModel;
using QuantConnect.Securities;

namespace Panoptes.ViewModels.Panels
{
    public sealed class CashViewModel : ObservableObject
    {
        public CashViewModel(Cash cash)
        {
            _amount = cash.Amount;
            _conversionRate = cash.ConversionRate;
            _currencySymbol = cash.CurrencySymbol;
            _symbol = cash.Symbol;
            _valueInAccountCurrency = cash.ValueInAccountCurrency;
        }

        public void Update(Cash cash)
        {
            Amount = cash.Amount;
            ConversionRate = cash.ConversionRate;
            CurrencySymbol = cash.CurrencySymbol;
            Symbol = cash.Symbol;
            ValueInAccountCurrency = cash.ValueInAccountCurrency;
        }

        private decimal _amount;
        public decimal Amount
        {
            get { return _amount; }
            private set
            {
                if (_amount == value) return;
                _amount = value;
                OnPropertyChanged();
            }
        }

        private decimal _conversionRate;
        public decimal ConversionRate
        {
            get { return _conversionRate; }
            private set
            {
                if (_conversionRate == value) return;
                _conversionRate = value;
                OnPropertyChanged();
            }
        }

        private decimal _valueInAccountCurrency;
        public decimal ValueInAccountCurrency
        {
            get { return _valueInAccountCurrency; }
            private set
            {
                if (_valueInAccountCurrency == value) return;
                _valueInAccountCurrency = value;
                OnPropertyChanged();
            }
        }

        private string _symbol;
        public string Symbol
        {
            get { return _symbol; }
            private set
            {
                if (_symbol == value) return;
                _symbol = value;
                OnPropertyChanged();
            }
        }

        private string _currencySymbol;
        public string CurrencySymbol
        {
            get { return _currencySymbol; }
            private set
            {
                if (_currencySymbol == value) return;
                _currencySymbol = value;
                OnPropertyChanged();
            }
        }
    }
}
