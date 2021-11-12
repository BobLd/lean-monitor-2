using Microsoft.Toolkit.Mvvm.ComponentModel;
using QuantConnect;
using QuantConnect.Orders;
using QuantConnect.Orders.TimeInForces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Panoptes.ViewModels.Panels
{
    public sealed class OrderViewModel : ObservableObject
    {
        private int _lastEventId = -1;

        private readonly Queue<OrderEvent> _pendingEvents = new Queue<OrderEvent>();
        private readonly Queue<string> _pendingBrokerIds = new Queue<string>();

        public OrderViewModel()
        { }

        public OrderViewModel(Order order) : this()
        {
            Events = new ObservableCollection<OrderEvent>();
            BrokerId = new ObservableCollection<string>();

            foreach (var id in order.BrokerId)
            {
                // Should be fine to do that in this thread
                // as the order does not exist yet and cannot
                // be displayed. Might change
                BrokerId.Add(id);
            }

            Id = order.Id;
            ContingentId = order.ContingentId;
            Symbol = order.Symbol;
            PriceCurrency = order.PriceCurrency;
            Time = order.Time;

            _price = order.Price;
            _lastFillTime = order.LastFillTime;
            _lastUpdateTime = order.LastUpdateTime;
            _canceledTime = order.CanceledTime;
            _quantity = order.Quantity;
            _type = order.Type;
            _tag = order.Tag;
            _properties = (OrderProperties)order.Properties;
            _orderSubmissionData = order.OrderSubmissionData;

            _status = order.Status;
            StatusStr = _status.ToShortString();

            if (order is LimitOrder limitOrder)
            {
                _limitPrice = limitOrder.LimitPrice;
            }
            else if (order is StopLimitOrder stopLimitOrder)
            {
                _limitPrice = stopLimitOrder.LimitPrice;
                _stopPrice = stopLimitOrder.StopPrice;
                _stopTriggered = stopLimitOrder.StopTriggered;
            }
            else if (order is StopMarketOrder stopMarketOrder)
            {
                _stopPrice = stopMarketOrder.StopPrice;
            }
            else if (order is LimitIfTouchedOrder limitIfTouchedOrder)
            {
                _triggerPrice = limitIfTouchedOrder.TriggerPrice;
                _limitPrice = limitIfTouchedOrder.LimitPrice;
                _triggerTouched = limitIfTouchedOrder.TriggerTouched;
            }
            // MarketOnCloseOrder, MarketOnOpenOrder, OptionExerciseOrder
        }

        public void Update(Order order)
        {
            if (Id != order.Id)
            {
                throw new ArgumentException($"Order Id is wrong. Expected {Id}, got {order.Id}.");
            }

            Debug.Assert(ContingentId == order.ContingentId);
            Debug.Assert(Symbol == order.Symbol);
            Debug.Assert(PriceCurrency == order.PriceCurrency);
            Debug.Assert(Time == order.Time);

            foreach (var id in order.BrokerId)
            {
                if (BrokerId.Contains(id)) continue;
                _pendingBrokerIds.Enqueue(id);
            }

            Price = order.Price;
            LastFillTime = order.LastFillTime;
            LastUpdateTime = order.LastUpdateTime;
            CanceledTime = order.CanceledTime;
            Quantity = order.Quantity;
            Type = order.Type;
            Status = order.Status;
            Tag = order.Tag;
            Properties = (OrderProperties)order.Properties;
            OrderSubmissionData = order.OrderSubmissionData;

            if (order is LimitOrder limitOrder)
            {
                LimitPrice = limitOrder.LimitPrice;
            }
            else if (order is StopLimitOrder stopLimitOrder)
            {
                LimitPrice = stopLimitOrder.LimitPrice;
                StopPrice = stopLimitOrder.StopPrice;
                StopTriggered = stopLimitOrder.StopTriggered;
            }
            else if (order is StopMarketOrder stopMarketOrder)
            {
                StopPrice = stopMarketOrder.StopPrice;
            }
            else if (order is LimitIfTouchedOrder limitIfTouchedOrder)
            {
                TriggerPrice = limitIfTouchedOrder.TriggerPrice;
                LimitPrice = limitIfTouchedOrder.LimitPrice;
                TriggerTouched = limitIfTouchedOrder.TriggerTouched;
            }
        }

        /// <summary>
        /// Return 'true' if updated. 'false' otherwise.
        /// </summary>
        /// <param name="orderEvent"></param>
        public bool Update(OrderEvent orderEvent)
        {
            if (Id != orderEvent.OrderId)
            {
                throw new ArgumentException($"OrderEvent Id is wrong. Expected {Id}, got {orderEvent.OrderId}.");
            }

            Debug.Assert(Symbol == orderEvent.Symbol);

            if (Events.Contains(orderEvent) || _lastEventId >= orderEvent.Id)
            {
                return false;
            }

            // We add the event in the UI thread -> BackgroundWorker.ProgressChanged
            // Add to queue and a FinishUpdate() function called from UI thread
            _pendingEvents.Enqueue(orderEvent);

            _lastEventId = orderEvent.Id;

            Status = orderEvent.Status;
            FilledQuantity += orderEvent.FillQuantity;           // TO CHECK
            Debug.Assert(Math.Abs(FilledQuantity) <= Math.Abs(Quantity));

            Fees = orderEvent.OrderFee.Value.Amount;             // TO CHECK
            FeesCurrency = orderEvent.OrderFee.Value.Currency;   // TO CHECK

            //orderEvent.FillPrice
            //orderEvent.FillPriceCurrency

            if (orderEvent.LimitPrice.HasValue && orderEvent.LimitPrice.Value != default)
            {
                LimitPrice = orderEvent.LimitPrice.Value;
            }

            if (orderEvent.StopPrice.HasValue && orderEvent.StopPrice.Value != default)
            {
                StopPrice = orderEvent.StopPrice.Value;
            }

            if (orderEvent.TriggerPrice.HasValue && orderEvent.TriggerPrice.Value != default)
            {
                TriggerPrice = orderEvent.TriggerPrice.Value;
            }
            return true;
        }

        /// <summary>
        /// Call this from UI thread (BackgroundWorker.ProgressChanged).
        /// </summary>
        public void FinishUpdateInThreadUI()
        {
            while (_pendingEvents.Count != 0)
            {
                Events.Add(_pendingEvents.Dequeue());
            }

            while (_pendingBrokerIds.Count != 0)
            {
                BrokerId.Add(_pendingBrokerIds.Dequeue());
            }
        }

        public ObservableCollection<OrderEvent> Events { get; }

        #region OrderEvent
        private decimal _fees;
        public decimal Fees
        {
            get { return _fees; }
            private set
            {
                if (_fees == value) return;
                _fees = value;
                OnPropertyChanged();
            }
        }

        private string _feesCurrency;
        public string FeesCurrency
        {
            get { return _feesCurrency; }
            private set
            {
                if (_feesCurrency == value) return;
                _feesCurrency = value;
                OnPropertyChanged();
            }
        }

        private decimal _filledQuantity;
        public decimal FilledQuantity
        {
            get { return _filledQuantity; }
            private set
            {
                if (_filledQuantity == value) return;
                _filledQuantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OpenQuantity));
                OnPropertyChanged(nameof(FilledPercent));
                OnPropertyChanged(nameof(FilledProgress));
            }
        }

        public decimal OpenQuantity => Quantity - FilledQuantity;

        public decimal? FilledPercent
        {
            get
            {
                if (Quantity == 0) return null;
                return Math.Abs(FilledQuantity / Quantity);
            }
        }

        /// <summary>
        /// From 0 to 100.
        /// </summary>
        public int FilledProgress
        {
            get
            {
                if (!FilledPercent.HasValue) return 0;
                return (int)(FilledPercent.Value * 100m);
            }
        }
        #endregion

        #region Order
        /// <summary>
        /// Order ID.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        public int ContingentId { get; }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        public ObservableCollection<string> BrokerId { get; }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        public Symbol Symbol { get; }

        private decimal _price;
        /// <summary>
        /// Price of the Order.
        /// </summary>
        public decimal Price
        {
            get { return _price; }
            private set
            {
                if (_price == value) return;
                _price = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(PriceChangePct));
            }
        }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        public string PriceCurrency { get; }

        /// <summary>
        /// Gets the utc time the order was created.
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// Gets the utc time this order was created. Alias for <see cref="Time"/>
        /// </summary>
        public DateTime CreatedTime => Time;

        private DateTime? _lastFillTime;
        /// <summary>
        /// Gets the utc time the last fill was received, or null if no fills have been received
        /// </summary>
        public DateTime? LastFillTime
        {
            get { return _lastFillTime; }
            private set
            {
                if (_lastFillTime == value) return;
                _lastFillTime = value;
                OnPropertyChanged();
            }
        }

        private DateTime? _lastUpdateTime;
        /// <summary>
        /// Gets the utc time this order was last updated, or null if the order has not been updated.
        /// </summary>
        public DateTime? LastUpdateTime
        {
            get { return _lastUpdateTime; }
            private set
            {
                if (_lastUpdateTime == value) return;
                _lastUpdateTime = value;
                OnPropertyChanged();
            }
        }

        private DateTime? _canceledTime;
        /// <summary>
        /// Gets the utc time this order was canceled, or null if the order was not canceled.
        /// </summary>
        public DateTime? CanceledTime
        {
            get { return _canceledTime; }
            private set
            {
                if (_canceledTime == value) return;
                _canceledTime = value;
                OnPropertyChanged();
            }
        }

        private decimal _quantity;
        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public decimal Quantity
        {
            get { return _quantity; }
            private set
            {
                if (_quantity == value) return;
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Direction));
                OnPropertyChanged(nameof(AbsoluteQuantity));
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(OpenQuantity));
                OnPropertyChanged(nameof(FilledPercent));
                OnPropertyChanged(nameof(FilledProgress));
                OnPropertyChanged(nameof(OrderSummary));
                OnPropertyChanged(nameof(IsMarketable));
            }
        }

        private OrderType _type;
        /// <summary>
        /// Order Type
        /// </summary>
        public OrderType Type
        {
            get { return _type; }
            private set
            {
                if (_type == value) return;
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OrderSummary));
                OnPropertyChanged(nameof(IsMarketable));
            }
        }

        private OrderStatus _status;
        /// <summary>
        /// Status of the Order
        /// </summary>
        public OrderStatus Status
        {
            get { return _status; }
            private set
            {
                if (_status == value) return;
                _status = value;
                StatusStr = _status.ToShortString();
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusStr));
            }
        }

        public string StatusStr { get; private set; }

        /// <summary>
        /// Order Time In Force
        /// </summary>
        public string TimeInForce
        {
            get
            {
                if (Properties.TimeInForce is GoodTilCanceledTimeInForce)
                {
                    return "GTC";
                }
                else if (Properties.TimeInForce is DayTimeInForce)
                {
                    return "Day";
                }
                else if (Properties.TimeInForce is GoodTilDateTimeInForce gtd)
                {
                    Expiry = gtd.Expiry;
                    return "GTD";
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }

        private DateTime? _expiry;
        public DateTime? Expiry
        {
            get { return _expiry; }
            private set
            {
                if (_expiry == value) return;
                _expiry = value;
                OnPropertyChanged();
            }
        }

        private string _tag;
        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        public string Tag
        {
            get { return _tag; }
            private set
            {
                if (_tag == value) return;
                _tag = value;
                OnPropertyChanged();
            }
        }

        private OrderProperties _properties;
        /// <summary>
        /// Additional properties of the order
        /// </summary>
        public OrderProperties Properties
        {
            get { return _properties; }
            private set
            {
                if (_properties == value) return;
                _properties = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeInForce));
            }
        }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        public SecurityType SecurityType => Symbol.ID.SecurityType;

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        public OrderDirection Direction
        {
            get
            {
                if (Quantity > 0)
                {
                    return OrderDirection.Buy;
                }
                else if (Quantity < 0)
                {
                    return OrderDirection.Sell;
                }
                return OrderDirection.Hold;
            }
        }

        /// <summary>
        /// Get the absolute quantity for this order
        /// </summary>
        public decimal AbsoluteQuantity => Math.Abs(Quantity);

        /// <summary>
        /// Gets the executed value of this order. If the order has not yet filled,
        /// then this will return zero.
        /// </summary>
        public decimal Value => Quantity * Price;

        private OrderSubmissionData _orderSubmissionData;
        /// <summary>
        /// Gets the price data at the time the order was submitted
        /// </summary>
        public OrderSubmissionData OrderSubmissionData
        {
            get { return _orderSubmissionData; }
            private set
            {
                if (_orderSubmissionData == value) return;
                _orderSubmissionData = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AskPrice));
                OnPropertyChanged(nameof(BidPrice));
                OnPropertyChanged(nameof(LastPrice));
                OnPropertyChanged(nameof(PriceChangePct));
                OnPropertyChanged(nameof(IsMarketable));
            }
        }

        public decimal? AskPrice => OrderSubmissionData?.AskPrice;
        public decimal? BidPrice => OrderSubmissionData?.BidPrice;
        public decimal? LastPrice => OrderSubmissionData?.LastPrice;

        public decimal? PriceChangePct
        {
            get
            {
                if (!LastPrice.HasValue || Price == 0) return null;
                return (Price / LastPrice.Value) - 1.0m;
            }
        }

        /// <summary>
        /// Returns true if the order is a marketable order.
        /// </summary>
        public bool IsMarketable
        {
            get
            {
                if (Type == OrderType.Limit)
                {
                    // check if marketable limit order using bid/ask prices
                    return OrderSubmissionData != null &&
                           ((Direction == OrderDirection.Buy && LimitPrice.Value >= OrderSubmissionData.AskPrice) ||
                            (Direction == OrderDirection.Sell && LimitPrice.Value <= OrderSubmissionData.BidPrice));
                }

                return Type == OrderType.Market;
            }
        }

        private decimal? _limitPrice;
        /// <summary>
        /// Limit price for this order.
        /// </summary>
        public decimal? LimitPrice
        {
            get { return _limitPrice; }
            private set
            {
                if (_limitPrice == value) return;
                _limitPrice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMarketable));
            }
        }

        private decimal? _stopPrice;
        /// <summary>
        /// Stop price for this stop market order.
        /// </summary>
        public decimal? StopPrice
        {
            get { return _stopPrice; }
            private set
            {
                if (_stopPrice == value) return;
                _stopPrice = value;
                OnPropertyChanged();
            }
        }

        private bool? _stopTriggered;
        /// <summary>
        /// Signal showing the "StopLimitOrder" has been converted into a Limit Order.
        /// </summary>
        public bool? StopTriggered
        {
            get { return _stopTriggered; }
            private set
            {
                if (_stopTriggered == value) return;
                _stopTriggered = value;
                OnPropertyChanged();
            }
        }

        private decimal? _triggerPrice;
        /// <summary>
        /// The price which, when touched, will trigger the setting of a limit order at <see cref="LimitPrice"/>.
        /// </summary>
        public decimal? TriggerPrice
        {
            get { return _triggerPrice; }
            private set
            {
                if (_triggerPrice == value) return;
                _triggerPrice = value;
                OnPropertyChanged();
            }
        }

        private bool? _triggerTouched;
        /// <summary>
        /// Whether or not the <see cref="TriggerPrice"/> has been touched.
        /// </summary>
        public bool? TriggerTouched
        {
            get { return _triggerTouched; }
            private set
            {
                if (_triggerTouched == value) return;
                _triggerTouched = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public string OrderSummary => $"#{Id} - {Direction} {Symbol} [{Type}]";
    }
}