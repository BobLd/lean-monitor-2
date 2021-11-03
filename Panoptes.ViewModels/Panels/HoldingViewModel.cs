using Microsoft.Toolkit.Mvvm.ComponentModel;
using QuantConnect;

namespace Panoptes.ViewModels.Panels
{
    public sealed class HoldingViewModel : ObservableObject
    {
        public HoldingViewModel(Holding holding)
        {
            _averagePrice = holding.AveragePrice;
            _conversionRate = holding.ConversionRate;
            _currencySymbol = holding.CurrencySymbol;
            _marketPrice = holding.MarketPrice;
            _marketValue = holding.MarketValue;
            _quantity = holding.Quantity;
            _symbol = holding.Symbol;
            _securityType = holding.Type;
            _unrealizedPnL = holding.UnrealizedPnL;
        }

        public void Update(Holding holding)
        {
            AveragePrice = holding.AveragePrice;
            ConversionRate = holding.ConversionRate;
            MarketPrice = holding.MarketPrice;
            MarketValue = holding.MarketValue;
            Quantity = holding.Quantity;
            UnrealizedPnL = holding.UnrealizedPnL;

            // Below should not change
            CurrencySymbol = holding.CurrencySymbol;
            Symbol = holding.Symbol;
            SecurityType = holding.Type;
        }

        private decimal _averagePrice;
        public decimal AveragePrice
        {
            get { return _averagePrice; }
            private set
            {
                if (_averagePrice == value) return;
                _averagePrice = value;
                OnPropertyChanged();
            }
        }

        private decimal? _conversionRate;
        public decimal? ConversionRate
        {
            get { return _conversionRate; }
            private set
            {
                if (_conversionRate == value) return;
                _conversionRate = value;
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

        private decimal _marketPrice;
        public decimal MarketPrice
        {
            get { return _marketPrice; }
            private set
            {
                if (_marketPrice == value) return;
                _marketPrice = value;
                OnPropertyChanged();
            }
        }

        private decimal _marketValue;
        public decimal MarketValue
        {
            get { return _marketValue; }
            private set
            {
                if (_marketValue == value) return;
                _marketValue = value;
                OnPropertyChanged();
            }
        }

        private decimal _quantity;
        public decimal Quantity
        {
            get { return _quantity; }
            private set
            {
                if (_quantity == value) return;
                _quantity = value;
                OnPropertyChanged();
            }
        }

        private Symbol _symbol;
        public Symbol Symbol
        {
            get { return _symbol; }
            private set
            {
                if (_symbol == value) return;
                _symbol = value;
                OnPropertyChanged();
            }
        }

        private SecurityType _securityType;
        public SecurityType SecurityType
        {
            get { return _securityType; }
            private set
            {
                if (_securityType == value) return;
                _securityType = value;
                OnPropertyChanged();
            }
        }

        private decimal _unrealizedPnL;
        public decimal UnrealizedPnL
        {
            get { return _unrealizedPnL; }
            private set
            {
                if (_unrealizedPnL == value) return;
                _unrealizedPnL = value;
                OnPropertyChanged();
            }
        }
    }
}
