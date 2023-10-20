using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Panoptes.Model.Sessions.Stream;
using QuantConnect;
using QuantConnect.Notifications;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Serialization;
using QuantConnect.Packets;
using QuantConnect.Securities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Panoptes.Model.Mock
{
    public class MockStreamingMessageHandler
    {
        private int _port;
        private PushSocket _server;
        private AlgorithmNodePacket _job;
        private OrderEventJsonConverter _orderEventJsonConverter;

        protected readonly BackgroundWorker _eternalQueueListener = new BackgroundWorker() { WorkerSupportsCancellation = true };
        protected readonly BackgroundWorker _queueReader = new BackgroundWorker() { WorkerSupportsCancellation = true };
        protected CancellationTokenSource _cts;

        protected readonly BlockingCollection<Packet> _packetQueue = new BlockingCollection<Packet>();

        /// <summary>
        /// Gets or sets whether this messaging handler has any current subscribers.
        /// This is not used in this message handler.  Messages are sent via tcp as they arrive
        /// </summary>
        public bool HasSubscribers { get; set; }

        public MockStreamingMessageHandler(StreamSessionParameters parameters)
        {
            if (!int.TryParse(parameters.Port, out var port))
            {
                throw new ArgumentOutOfRangeException("The port should be an integer.", nameof(port));
            }
            _port = port;

            _startTime = DateTime.UtcNow;
            _currentTime = _startTime;
            _orderEventJsonConverter = new OrderEventJsonConverter(AlgorithmId);
        }

        /// <summary>
        /// Initialize the messaging system
        /// </summary>
        public void Initialize()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };

            CheckPort();
            _server = new PushSocket($"@tcp://*:{_port}");

            _cts = new CancellationTokenSource();

            // Configure the worker threads
            //_eternalQueueListener.WorkerSupportsCancellation = true;
            _eternalQueueListener.DoWork += EventsListener;
            _eternalQueueListener.RunWorkerAsync();

            //_queueReader.WorkerSupportsCancellation = true;
            _queueReader.DoWork += QueueReader;
            _queueReader.RunWorkerAsync();
        }

        /// <summary>
        /// Set the user communication channel
        /// </summary>
        /// <param name="job"></param>
        public void SetAuthentication(AlgorithmNodePacket job)
        {
            _job = job;
            _orderEventJsonConverter = new OrderEventJsonConverter(job.AlgorithmId);
            Transmit(_job);
        }

        /// <summary>
        /// Send any notification with a base type of Notification.
        /// </summary>
        /// <param name="notification">The notification to be sent.</param>
        public void SendNotification(Notification notification)
        {
            var type = notification.GetType();
            if (type == typeof(NotificationEmail) || type == typeof(NotificationWeb) || type == typeof(NotificationSms) || type == typeof(NotificationTelegram))
            {
                Trace.TraceError("Messaging.SendNotification(): Send not implemented for notification of type: " + type.Name);
                return;
            }
            notification.Send();
        }

        /// <summary>
        /// Send all types of packets
        /// </summary>
        public void Send(Packet packet)
        {
            Transmit(packet);
        }

        /// <summary>
        /// Send a message to the _server using ZeroMQ
        /// </summary>
        /// <param name="packet">Packet to transmit</param>
        public void Transmit(Packet packet)
        {
            var payload = JsonConvert.SerializeObject(packet, Formatting.None, _orderEventJsonConverter);

            var message = new NetMQMessage();

            message.Append(payload, Encoding.UTF8);

            _server.SendMultipartMessage(message);
        }

        /// <summary>
        /// Check if port to be used by the desktop application is available.
        /// </summary>
        private void CheckPort()
        {
            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, _port);
                tcpListener.Start();
                tcpListener.Stop();
            }
            catch
            {
                throw new Exception("The port configured in config.json is either being used or blocked by a firewall." +
                    "Please choose a new port or open the port in the firewall.");
            }
        }

        protected virtual void QueueReader(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (!_queueReader.CancellationPending && !_cts.Token.IsCancellationRequested)
                {
                    var p = _packetQueue.Take(_cts.Token);
                    Transmit(p);
                }
            }
            catch (OperationCanceledException)
            { }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //_resetEvent.Set();
            }
        }

        protected void EventsListener(object sender, DoWorkEventArgs e)
        {
            int deltaMinutes = _random.Next(1, 60);
            _currentTime = _currentTime.AddMinutes(deltaMinutes);

            var values = Enum.GetValues(typeof(PacketType));

            _packetQueue.Add(GetAlgorithmStatusPacket(AlgorithmStatus.RuntimeError));
            Thread.Sleep(200);
            _packetQueue.Add(GetAlgorithmStatusPacket(AlgorithmStatus.InQueue));
            Thread.Sleep(200);

            _packetQueue.Add(GetAlgorithmStatusPacket(AlgorithmStatus.History));
            for (int s = 0; s < (deltaMinutes * 60 / _stepSecond); s++)
            {
                NextChartStep();
                NextPriceStep();
            }

            _packetQueue.Add(GetLiveNodePacket());
            Thread.Sleep(200);

            _packetQueue.Add(GetAlgorithmStatusPacket(AlgorithmStatus.LoggingIn));
            Thread.Sleep(200);
            _packetQueue.Add(GetAlgorithmStatusPacket(AlgorithmStatus.Initializing));
            Thread.Sleep(200);

            // Live results here
            _packetQueue.Add(GetLiveResultPacket());
            Thread.Sleep(200);

            _packetQueue.Add(GetSecurityTypesPacket());
            Thread.Sleep(200);

            _packetQueue.Add(GetAlgorithmStatusPacket(AlgorithmStatus.Running));
            Thread.Sleep(200);

            _packetQueue.Add(GetDebugPacket($"Launching analysis for {AlgorithmId} with LEAN Engine v2.5.0.0"));
            _packetQueue.Add(GetDebugPacket("Mock Brokerage account base currency: USD"));

            while (_cts?.IsCancellationRequested == false)
            {
                NextPriceStep();
                // Always send a LiveResult packet
                _packetQueue.Add(GetLiveResultPacket());

                switch ((PacketType)values.GetValue(_random.Next(values.Length)))
                {
                    case PacketType.AlgorithmStatus:
                        //_packetQueue.Add(GetAlgorithmStatusPacket());
                        break;

                    case PacketType.LiveNode:
                        //_packetQueue.Add(GetLiveNodePacket());
                        break;

                    //case PacketType.AlgorithmNode:
                    //    _packetQueue.Add(JsonConvert.DeserializeObject<AlgorithmNodePacket>(payload));
                    //    break;

                    case PacketType.LiveResult:
                        _packetQueue.Add(GetLiveResultPacketOrders());
                        break;

                    //case PacketType.BacktestResult:
                    //    _packetQueue.Add(JsonConvert.DeserializeObject<BacktestResultPacket>(payload, orderConverterId));
                    //    break;

                    case PacketType.OrderEvent:
                        _packetQueue.Add(GetOrderEventPacket());
                        _packetQueue.Add(GetOrderEventPacket());
                        _packetQueue.Add(GetOrderEventPacket());
                        break;

                    case PacketType.Log:
                        _packetQueue.Add(GetLogPacket());
                        break;

                    case PacketType.Debug:
                        _packetQueue.Add(GetDebugPacket());
                        break;

                    case PacketType.HandledError:
                        _packetQueue.Add(GetHandledErrorPacket());
                        break;
                }

                _currentTime = _currentTime.AddSeconds(_stepSecond + _random.Next(0, 3));
                Thread.Sleep(_sleep);
            }
        }

        #region Mock data
        private static readonly Random _random = new Random();
        private readonly DateTime _startTime;
        private DateTime _currentTime;
        private readonly int _sleep = 50; // ms
        private readonly int _stepSecond = 1;

        private readonly string[] _symbols = new string[] { "ALGOEUR XJ", "ALGOGBP XJ", "ALGOUSD XJ", "ATOMBTC XJ", "ATOMUSD XJ", "BALBTC XJ", "BALUSD XJ", "BANDBTC XJ", "BANDEUR XJ", "BANDGBP XJ", "BANDUSD XJ", "BATETH XJ", "BATUSDC XJ", "BCHBTC XJ", "BCHEUR XJ", "BCHGBP XJ", "BCHUSD XJ", "BTCEUR XJ", "BTCGBP XJ", "BTCUSD XJ", "BTCUSDC XJ", "CGLDBTC XJ", "CGLDEUR XJ", "CGLDGBP XJ", "CGLDUSD XJ", "COMPBTC XJ", "COMPUSD XJ", "CVCUSDC XJ", "DAIUSD XJ", "DAIUSDC XJ", "DASHBTC XJ", "DASHUSD XJ", "DNTUSDC XJ", "EOSBTC XJ", "EOSEUR XJ", "EOSUSD XJ", "ETCBTC XJ", "ETCEUR XJ", "ETCGBP XJ", "ETCUSD XJ", "ETHBTC XJ", "ETHDAI XJ", "ETHEUR XJ", "ETHGBP XJ", "ETHUSD XJ", "ETHUSDC XJ", "GNTUSDC XJ", "KNCBTC XJ", "KNCUSD XJ", "LINKETH XJ", "LINKEUR XJ", "LINKGBP XJ", "LINKUSD XJ", "LOOMUSDC XJ", "LRCUSD XJ", "LTCBTC XJ", "LTCEUR XJ", "LTCGBP XJ", "LTCUSD XJ", "MANAUSDC XJ", "MKRBTC XJ", "MKRUSD XJ", "NMRBTC XJ", "NMREUR XJ", "NMRGBP XJ", "NMRUSD XJ", "OMGBTC XJ", "OMGEUR XJ", "OMGGBP XJ", "OMGUSD XJ", "OXTUSD XJ", "RENBTC XJ", "RENUSD XJ", "REPBTC XJ", "REPUSD XJ", "UMABTC XJ", "UMAEUR XJ", "UMAGBP XJ", "UMAUSD XJ", "UNIUSD XJ", "USDCEUR XJ", "USDCGBP XJ", "WBTCBTC XJ", "WBTCUSD XJ", "XLMBTC XJ", "XLMEUR XJ", "XLMUSD XJ", "XRPBTC XJ", "XRPEUR XJ", "XRPGBP XJ", "XRPUSD XJ", "XTZBTC XJ", "XTZEUR XJ", "XTZGBP XJ", "XTZUSD XJ", "YFIUSD XJ", "ZECBTC XJ", "ZECUSDC XJ", "ZRXBTC XJ", "ZRXEUR XJ", "ZRXUSD XJ" };

        private int _orderId;

        private readonly ConcurrentDictionary<int, Order> _orders = new ConcurrentDictionary<int, Order>();

        private readonly Dictionary<string, decimal> _lastSeriesPoint = new Dictionary<string, decimal>();
        private readonly Dictionary<string, decimal> _lastPrice = new Dictionary<string, decimal>();
        private const decimal maximumPercentDeviation = 0.1m;

        public const string HostName = "MOCK-HOST-NAME";
        public const string AlgorithmId = "mock-algo-id";
        public const int ProjectId = 42;
        public const int UserId = 99;
        public const string Channel = "Channel-algo-status";
        public const string CompileId = "CompileId-algo-status";
        public const string SessionId = "SessionId-algo-status";
        public const string DeployId = "DeployId-algo-status";
        private readonly Dictionary<string, (string, SeriesType, ScatterMarkerSymbol?)[]> Charts = new Dictionary<string, (string, SeriesType, ScatterMarkerSymbol?)[]>()
        {
            { "Strategy Equity", new (string, SeriesType, ScatterMarkerSymbol?)[] { ("Equity", (SeriesType)2, null) } },
            { "MACD",            new (string, SeriesType, ScatterMarkerSymbol?)[] { ("Price", (SeriesType)2, null), ("MACD-10d", SeriesType.Line, null), ("MACD-100d", SeriesType.Line, null) } },
            { "Markers",         new (string, SeriesType, ScatterMarkerSymbol?)[] { ("Line-Diamond", (SeriesType)2, ScatterMarkerSymbol.Diamond), ("Scatter-Square", SeriesType.Scatter, ScatterMarkerSymbol.Square), ("Line-Null", SeriesType.Line, null) } }
        };

        private static readonly Array algoStatus = Enum.GetValues(typeof(AlgorithmStatus));
        private static readonly Array orderTypes = Enum.GetValues(typeof(OrderType));
        private static readonly Array orderStatus = Enum.GetValues(typeof(OrderStatus));

        private static SecurityTypesPacket GetSecurityTypesPacket()
        {
            return JsonConvert.DeserializeObject<SecurityTypesPacket>("{'aMarkets':[7],'TypesCSV':'crypto','eType':'SecurityTypes','sChannel':''}");
        }

        private static AlgorithmStatusPacket GetAlgorithmStatusPacket(AlgorithmStatus? status = null)
        {
            if (!status.HasValue)
            {
                status = (AlgorithmStatus)algoStatus.GetValue(_random.Next(algoStatus.Length));
            }

            return new AlgorithmStatusPacket(AlgorithmId, ProjectId, status.Value, $"Message for algo status {status}")
            {
                Channel = Channel
            };
        }

        private LiveResultPacket GetLiveResultPacket()
        {
            return new LiveResultPacket()
            {
                CompileId = CompileId,
                Channel = Channel,
                SessionId = SessionId,
                DeployId = DeployId,
                UserId = UserId,
                ProjectId = ProjectId,
                ProcessingTime = _random.NextDouble() * 100,
                Results = new LiveResult(GetLiveResultParameters()),
            };
        }

        private LiveResultParameters GetLiveResultParameters()
        {
            return new LiveResultParameters(
                Charts.Select(c => GetChart(c.Key, c.Value)).ToDictionary(k => k.Name, k => k),
                null,
                GetProfitLoss(),
                GetHoldings(),
                GetCashBook(),
                null,
                GetRuntimeStatistics(),
                null,
                GetServerStatistics(),
                null);

            // What about orderevent in here??
        }

        public static CashBook GetCashBook()
        {
            return new CashBook
            {
                ["EUR"] = new Cash("EUR", _random.Next(5_000_000, 10_000_000), (decimal)(_random.NextDouble() * 10)),
                ["USD"] = new Cash("USD", _random.Next(5_000, 1_000_000), (decimal)(_random.NextDouble() * 10)),
                ["BTC"] = new Cash("BTC", _random.Next(10, 10_000), (decimal)(_random.NextDouble() * 10)),
                ["GBP"] = new Cash("GBP", _random.Next(5_000_000, 10_000_000), (decimal)(_random.NextDouble() * 10)),
                ["ETH"] = new Cash("ETH", _random.Next(10, 10_000), (decimal)(_random.NextDouble() * 10)),
            };
        }

        public Dictionary<string, Holding> GetHoldings()
        {
            var hlds = new Dictionary<string, Holding>();
            foreach (var symbol in _symbols)
            {
                var sid = SecurityIdentifier.Parse(symbol);
                decimal qty = (decimal)_random.Next(0, 1_000);
                decimal price = _lastPrice[symbol];
                hlds[symbol] = new Holding()
                {
                    Symbol = new Symbol(sid, sid.Symbol),
                    AveragePrice = _lastPrice[symbol] * (decimal)(1 + _random.Next() - 0.5),
                    MarketPrice = price,
                    CurrencySymbol = symbol.Substring(0, 3),
                    ConversionRate = (decimal)(1 + _random.Next() - 0.5),
                    Quantity = qty,
                    MarketValue = qty * price,
                    UnrealizedPnL = _random.Next() * (decimal)_random.NextDouble()
                };
            }
            return hlds;
        }

        /// <summary>
        /// Gets the statistics of the machine, including CPU% and RAM
        /// </summary>
        public Dictionary<string, string> GetServerStatistics()
        {
            var upTime = _currentTime - _startTime;
            return new Dictionary<string, string>
            {
                { "CPU Usage", $"{_random.NextDouble() * 100:0.0}%" },
                { "Used RAM (MB)", $"{_random.Next(100, 2000)}" },
                { "Total RAM (MB)", "2000" },
                { "Hostname", HostName },
                { "LEAN Version", $"v{Globals.Version}"},
                { "Up Time", $"{upTime.Days}d {upTime:hh\\:mm\\:ss}" },
            };
        }

        private Dictionary<DateTime, decimal> GetProfitLoss()
        {
            if (_currentTime.Second > 9) return null;

            return new Dictionary<DateTime, decimal>()
            {
                { _currentTime.AddSeconds(-_stepSecond / 2.0), (decimal)(_random.NextDouble() - 0.5) * 2m },
                { _currentTime, (decimal)(_random.NextDouble() - 0.5) * 2m }
            };
        }

        private LiveResultPacket GetLiveResultPacketOrders()
        {
            return new LiveResultPacket()
            {
                CompileId = CompileId,
                Channel = Channel,
                SessionId = SessionId,
                DeployId = DeployId,
                UserId = UserId,
                ProjectId = ProjectId,
                ProcessingTime = _random.NextDouble() * 100.0,
                Results = new LiveResult(GetLiveResultParametersOrders())
            };
        }

        private LiveResultParameters GetLiveResultParametersOrders()
        {
            return new LiveResultParameters(
                null,
                GetOrders().ToDictionary(k => k.Id, k => k),
                null, null, null, null,
                null, null, null, null);

            // What about orderevent in here??
        }

        private Dictionary<string, string> GetRuntimeStatistics()
        {
            if (_currentTime.Second.ToString().StartsWith("1") &&
                _lastSeriesPoint.ContainsKey("Strategy Equity-Equity"))
            {
                decimal equity = _lastSeriesPoint["Strategy Equity-Equity"];
                decimal perf = (decimal)((_random.NextDouble() - 0.5) * 0.2);
                decimal netProfit = perf * equity;
                decimal volume = equity * (decimal)_random.NextDouble() * 0.1m;
                decimal fees = -volume * 0.01m;
                decimal unrealized = ((decimal)_random.NextDouble() - 0.5m) * netProfit;
                decimal sr = perf / 0.4m;
                decimal holdings = equity * (decimal)_random.NextDouble();
                return new Dictionary<string, string>
                {
                    { "Probabilistic Sharpe Ratio", sr.ToString("0.###%") },
                    { "Unrealized", unrealized.ToString("C2") },
                    { "Fees", fees.ToString("C2") },
                    { "Net Profit", netProfit.ToString("C2") },
                    { "Return", perf.ToString("0.##%") },
                    { "Equity", equity.ToString("C2") },
                    { "Holdings", holdings.ToString("C2") },
                    { "Volume", volume.ToString("C2") }
                };
            }

            return new Dictionary<string, string>();
        }

        private OrderEventPacket GetOrderEventPacket()
        {
            return new OrderEventPacket(AlgorithmId, GetOrderEvent());
        }

        private OrderEvent GetOrderEvent()
        {
            if (_orders.IsEmpty) return null;
            var order = _orders[_random.Next(1, _orderId)];

            decimal lastPrice = _lastPrice[order.Symbol.ToString()];
            decimal fillPrice = 0;
            decimal fillQty = 0;
            var fees = new OrderFee(new CashAmount());

            var status = (OrderStatus)orderStatus.GetValue(_random.Next(orderStatus.Length));
            string message = $"{status}-{new string(Enumerable.Repeat(chars, _random.Next(5, 15)).Select(s => s[_random.Next(s.Length)]).ToArray())}";

            switch (status)
            {
                case OrderStatus.Filled:
                    fillPrice = Math.Round(order.Direction == OrderDirection.Buy ? lastPrice * 1.02m : order.Direction == OrderDirection.Sell ? lastPrice * 0.98m : lastPrice, 4);
                    fillQty = order.Quantity;
                    fees = new OrderFee(new CashAmount(fillPrice * fillQty * 0.01m, order.PriceCurrency));
                    break;

                case OrderStatus.None:
                case OrderStatus.PartiallyFilled:
                    status = OrderStatus.PartiallyFilled;
                    fillPrice = Math.Round(order.Direction == OrderDirection.Buy ? lastPrice * 1.02m : order.Direction == OrderDirection.Sell ? lastPrice * 0.98m : lastPrice, 4);
                    fillQty = Math.Round(order.Quantity * (decimal)_random.NextDouble(), 4);
                    fees = new OrderFee(new CashAmount(fillPrice * fillQty * 0.01m, order.PriceCurrency));
                    break;

                case OrderStatus.Invalid:
                case OrderStatus.UpdateSubmitted:
                case OrderStatus.Submitted:
                case OrderStatus.Canceled:
                case OrderStatus.CancelPending:
                default:
                    break;

                case OrderStatus.New:
                    return null;
            }

            return new OrderEvent(order.Id, order.Symbol,
                                  _currentTime, status,
                                  order.Direction, fillPrice,
                                  fillQty, fees, message);
        }

        private IEnumerable<Order> GetOrders()
        {
            if (_currentTime.Second % 2 == 0)
            {
                int orderCount = _random.Next(1, 5);

                for (int i = 0; i < orderCount; i++)
                {
                    var order = GetOrder(++_orderId);
                    if (_orders.TryAdd(order.Id, order))
                    {
                        //if (order.Direction == OrderDirection.Hold) yield break;
                        yield return order;
                    }
                }
            }
            else
            {
                yield break;
                /*
                if (_orders.Count == 0) yield break;
                var order = _orders[_random.Next(1, _orderId)];
                order.ApplyUpdateOrderRequest(new UpdateOrderRequest(_currentTime, order.Id, new UpdateOrderFields()
                {
                    Tag = "Update tag, " + order.Tag
                }));

                yield return order;
                */
            }
        }

        private Order GetOrder(int id)
        {
            var symbolStr = _symbols[_random.Next(_symbols.Length)];
            var sid = SecurityIdentifier.Parse(symbolStr);
            var symbol = new Symbol(sid, sid.Symbol);
            var type = (OrderType)orderTypes.GetValue(_random.Next(orderTypes.Length));
            var tag = new string(Enumerable.Repeat(chars, _random.Next(15, 60)).Select(s => s[_random.Next(s.Length)]).ToArray());
            var qty = Math.Round((decimal)(_random.NextDouble() * 10) * _random.Next(-1, 2), 4);

            decimal lastPrice = _lastPrice[symbolStr];
            decimal limitPrice = lastPrice * 0.95m;
            decimal stopPrice = lastPrice * 1.05m;
            decimal triggerPrice = lastPrice * 1.02m;

            MockSubmitOrderRequest request;
            switch (type)
            {
                case OrderType.Limit:
                    request = new MockSubmitOrderRequest(id, type, SecurityType.Crypto, symbol, qty, 0, limitPrice, _currentTime, tag);
                    break;

                case OrderType.OptionExercise:
                    // TO DO
                    request = new MockSubmitOrderRequest(id, OrderType.Market, SecurityType.Crypto, symbol, qty, 0, 0, _currentTime, tag);
                    break;

                case OrderType.StopLimit:
                    request = new MockSubmitOrderRequest(id, type, SecurityType.Crypto, symbol, qty, stopPrice, limitPrice, _currentTime, tag);
                    break;

                case OrderType.StopMarket:
                    request = new MockSubmitOrderRequest(id, type, SecurityType.Crypto, symbol, qty, stopPrice, 0, _currentTime, tag);
                    break;

                case OrderType.LimitIfTouched:
                    request = new MockSubmitOrderRequest(id, type, SecurityType.Crypto, symbol, qty, 0, limitPrice, triggerPrice, _currentTime, tag);
                    break;

                case OrderType.Market:
                case OrderType.MarketOnClose:
                case OrderType.MarketOnOpen:
                default:
                    request = new MockSubmitOrderRequest(id, type, SecurityType.Crypto, symbol, qty, 0, 0, _currentTime, tag);
                    break;
            }

            var response = OrderResponse.Success(request);
            request.SetResponse(response, OrderRequestStatus.Processed);
            return Order.CreateOrder(request);
        }

        private Chart GetChart(string name, params (string, SeriesType, ScatterMarkerSymbol?)[] series)
        {
            var se = series.Select(s => GetSeries(name, s.Item1, s.Item2, s.Item3)).ToDictionary(k => k.Name, k => (BaseSeries) k);
            return new Chart(name)
            {
                Series = se,
            };
        }

        private Series GetSeries(string chartName, string seriesName, SeriesType seriesType, ScatterMarkerSymbol? scatterMarkerSymbol)
        {
            string key = $"{chartName}-{seriesName}";
            // from https://github.com/QuantConnect/Lean/blob/master/ToolBox/RandomDataGenerator/RandomValueGenerator.cs#L13
            decimal referencePrice;
            Color color = Color.Black;
            if (!_lastSeriesPoint.ContainsKey(key))
            {
                referencePrice = (decimal)(_random.NextDouble() * _random.Next(1, 5_000));
                _lastSeriesPoint.Add(key, referencePrice);
                color = Color.FromArgb(_random.Next(0, 256), _random.Next(0, 256), _random.Next(0, 256));
            }

            NextChartStep();

            var series = new Series(seriesName, seriesType)
            {
                Values = new List<ISeriesPoint>()
                {
                    // Just one point for the moment
                    new ChartPoint(_currentTime,  _lastSeriesPoint[key])
                },
                Color = color
            };

            if (scatterMarkerSymbol.HasValue)
            {
                series.ScatterMarkerSymbol = scatterMarkerSymbol.Value;
            }

            if (chartName == "MACD" && seriesName == "Price")
            {
                series.Unit = "₿";
            }

            return series;
        }

        private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz   0123456789 ";
        private static LogPacket GetLogPacket()
        {
            return new LogPacket()
            {
                AlgorithmId = AlgorithmId,
                Channel = Channel,
                Message = new string(Enumerable.Repeat(chars, _random.Next(15, 250)).Select(s => s[_random.Next(s.Length)]).ToArray())
            };
        }

        private static DebugPacket GetDebugPacket(string message = null, bool toast = false)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = new string(Enumerable.Repeat(chars, _random.Next(15, 60)).Select(s => s[_random.Next(s.Length)]).ToArray());
            }

            Debug.Assert(!string.IsNullOrEmpty(message));

            return new DebugPacket(ProjectId, AlgorithmId, CompileId, message, toast);
        }

        private static HandledErrorPacket GetHandledErrorPacket()
        {
            return new HandledErrorPacket(AlgorithmId,
                new string(Enumerable.Repeat(chars, _random.Next(15, 60)).Select(s => s[_random.Next(s.Length)]).ToArray()),
                $"StackTrace - {new string(Enumerable.Repeat(chars, _random.Next(15, 60)).Select(s => s[_random.Next(s.Length)]).ToArray())}");
        }

        private static LiveNodePacket GetLiveNodePacket()
        {
            return new LiveNodePacket()
            {
                Algorithm = new byte[] { 0, 1, 2 },
                Brokerage = "MockBrokerage",
                BrokerageData = new Dictionary<string, string>() { { "mock-api-key", "sdfasfdiwohfvb" } },
                Channel = Channel,
                CompileId = CompileId,
                Controls = new Controls
                {
                    MinuteLimit = 100,
                    SecondLimit = 50,
                    TickLimit = 25,
                    RamAllocation = 512
                },
                DataChannelProvider = "DataChannelProvider",
                DataQueueHandler = "MockDataQueueHandler",
                DeployId = DeployId,
                DisableAcknowledgement = false,
                HistoryProvider = "MockBrokerageHistoryProvider",
                HostName = HostName,
                Language = Language.CSharp,
                LiveDataTypes = null,
                NotificationEvents = null,
                NotificationTargets = null,
                OrganizationId = "",
                Parameters = new Dictionary<string, string>()
                {
                    { "intrinio-username", "" },
                    { "intrinio-password", "" },
                    { "ema-fast", "10" },
                    { "ema-slow", "20" }
                },
                ProjectId = ProjectId,
                ProjectName = null,
                Redelivered = false,
                RequestSource = "WebIDE",
                ServerType = ServerType.Server1024,
                SessionId = SessionId,
                UserId = UserId,
                UserToken = "",
                Version = Globals.Version
            };
        }

        private void NextChartStep()
        {
            foreach (var key in _lastSeriesPoint.Keys)
            {
                var price = _lastSeriesPoint[key];
                decimal change = (0.0025m * ((1_000m / price) - 1m)) + (maximumPercentDeviation * (decimal)(_random.NextDouble() - 0.499));
                _lastSeriesPoint[key] = price * (1m + change);
            }
        }

        private void NextPriceStep()
        {
            foreach (var symbol in _symbols)
            {
                if (!_lastPrice.ContainsKey(symbol))
                {
                    decimal referencePrice = Math.Round((decimal)(_random.NextDouble() * _random.Next(1, 5_000)), 4);
                    _lastPrice.Add(symbol, referencePrice);
                }
                else
                {
                    decimal price = _lastPrice[symbol];
                    decimal change = (0.0025m * ((1_000m / price) - 1m)) + (maximumPercentDeviation * (decimal)(_random.NextDouble() - 0.499));
                    _lastPrice[symbol] = price * (1m + change);
                }
            }
        }
        #endregion
    }
}
