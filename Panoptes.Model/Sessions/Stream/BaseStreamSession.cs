using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuantConnect.Packets;
using QuantConnect.Orders;
using QuantConnect.Orders.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using PacketType = QuantConnect.Packets.PacketType;



namespace Panoptes.Model.Sessions.Stream
{
    public abstract class BaseStreamSession : ISession
    {
        protected readonly ILogger _logger;

        private int counter = 0;
        private int counter_qp = 0;
        protected readonly ISessionHandler _sessionHandler;
        protected readonly IResultConverter _resultConverter;

        protected readonly BackgroundWorker _eternalQueueListener = new BackgroundWorker() { WorkerSupportsCancellation = true };
        protected readonly BackgroundWorker _queueReader = new BackgroundWorker() { WorkerSupportsCancellation = true };
        protected CancellationTokenSource _cts;

        protected readonly BlockingCollection<Packet> _packetQueue = new BlockingCollection<Packet>();

        protected readonly SynchronizationContext _syncContext;

        protected readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        protected readonly string _host;
        protected readonly int _port;
        protected readonly bool _closeAfterCompleted;
        protected JsonSerializerSettings _jsonSettings;
        protected OrderEventJsonConverter _orderEventJsonConverter;
        protected string _algorithmId;

        public string Name => $"{_host}:{_port}";

        public BaseStreamSession(ISessionHandler sessionHandler, IResultConverter resultConverter,
            StreamSessionParameters parameters, ILogger logger)
        {
            _logger = logger;

            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            _sessionHandler = sessionHandler;
            _resultConverter = resultConverter;

            _host = parameters.Host;
            if (!int.TryParse(parameters.Port, out var port))
            {
                throw new ArgumentOutOfRangeException(nameof(port), "The port should be an integer.");
            }
            _port = port;
            _closeAfterCompleted = parameters.CloseAfterCompleted;

            _syncContext = SynchronizationContext.Current;

            if (_syncContext == null)
            {
                throw new NullReferenceException("BaseStreamSession: SynchronizationContext.Current is null, please make sure the session was created in UI thread.");
            }

            _jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None, 
                Converters = { new OrderJsonConverter() }
            };
            // OrderEventJsonConverter initialized after receiving AlgorithmId
        }

        public virtual void Initialize()
        {
            Subscribe();
        }

        public virtual Task InitializeAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => Initialize(), cancellationToken);
        }

        public virtual void Shutdown()
        {
            Unsubscribe();
        }

        public virtual void Subscribe()
        {
            try
            {
                if (_cts == null)
                {
                    _cts = new CancellationTokenSource();

                    // Configure the worker threads
                    _eternalQueueListener.DoWork += EventsListener;
                    _eternalQueueListener.RunWorkerAsync();

                    _queueReader.DoWork += QueueReader;
                    _queueReader.RunWorkerAsync();

                    State = SessionState.Subscribed;
                    _logger.LogInformation("BaseStreamSession.Subscribe: New subscription.");
                }
                else
                {
                    _logger.LogInformation("BaseStreamSession.Subscribe: Cannot subscribe because aslready subscribed.");
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not subscribe to the stream", e);
            }
        }

        public virtual void Unsubscribe()
        {
            try
            {
                if (_eternalQueueListener != null) // should never be the case - check if working?
                {
                    _eternalQueueListener.CancelAsync();
                    _eternalQueueListener.DoWork -= EventsListener;
                }

                if (_queueReader != null) // should never be the case - check if working?
                {
                    _queueReader.CancelAsync();
                    _queueReader.DoWork -= QueueReader;
                }

                _cts?.Cancel();
                State = SessionState.Unsubscribed;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not unsubscribe from the {this.GetType()}", e);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        #region Queue Reader
        protected virtual void QueueReader(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (!_queueReader.CancellationPending && !_cts.Token.IsCancellationRequested)
                {
                    var packet = _packetQueue.Take(_cts.Token);
                    HandlePacketQueueReader(packet);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("QueueReader operation was cancelled.");
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        protected bool HandlePacketQueueReader(Packet packet)
        {

            _logger.LogDebug("HandlePacketQueueReader: Handling packet of type '{PacketType}'", packet.Type.ToString());

            switch (packet.Type)
            {
                case PacketType.AlgorithmStatus:
                    _syncContext.Send(_ => _sessionHandler.HandleAlgorithmStatus((AlgorithmStatusPacket)packet), null);
                    break;

                case PacketType.LiveNode:
                    _syncContext.Send(_ => _sessionHandler.HandleLiveNode((LiveNodePacket)packet), null);
                    break;

                case PacketType.AlgorithmNameUpdate:
                    //TODO
                    break;

                case PacketType.AlgorithmNode:
                    var algorithmNodePacket = (AlgorithmNodePacket)packet;
                    //TODO
                    break;

                case PacketType.LiveResult:
                    HandleLiveResultPacket(packet);
                    break;

                case PacketType.BacktestResult:
                    HandleBacktestResultPacket(packet);
                    break;

                case PacketType.Log:
                    _syncContext.Send(_ => _sessionHandler.HandleLogMessage(DateTime.UtcNow, ((LogPacket)packet).Message, LogItemType.Log), null);
                    break;

                case PacketType.Debug:
                    _syncContext.Send(_ => _sessionHandler.HandleLogMessage(DateTime.UtcNow, ((DebugPacket)packet).Message, LogItemType.Debug), null);
                    break;

                case PacketType.HandledError:
                    _syncContext.Send(_ => _sessionHandler.HandleLogMessage(DateTime.UtcNow, ((HandledErrorPacket)packet).Message, LogItemType.Error), null);
                    break;

                case PacketType.OrderEvent:
                    _syncContext.Send(_ => _sessionHandler.HandleOrderEvent((OrderEventPacket)packet), null);
                    break;

                default:
                    _logger.LogWarning("HandlePacketQueueReader: Unknown packet '{Packet}'", packet);
                    return false;
            }

            return true;
        }

        private void HandleBacktestResultPacket(Packet packet)
        {
            var backtestResultPacket = (BacktestResultPacket)packet;
            var backtestResultUpdate = _resultConverter.FromBacktestResult(backtestResultPacket.Results);

            var context = new ResultContext
            {
                Name = Name,
                Result = backtestResultUpdate,
                Progress = backtestResultPacket.Progress
            };
            _syncContext.Post(_ =>
            {
                try
                {
                    _sessionHandler.HandleResult(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling result context");
                }
            }, null);

            if (backtestResultPacket.Progress == 1 && _closeAfterCompleted)
            {
                _syncContext.Send(_ => Unsubscribe(), null);
            }
        }

        private void HandleLiveResultPacket(Packet packet)
        {
            var liveResultPacket = (LiveResultPacket)packet;
            var liveResultUpdate = _resultConverter.FromLiveResult(liveResultPacket.Results);

            var context = new ResultContext
            {
                Name = Name,
                Result = liveResultUpdate
            };

            _syncContext.Send(_ => _sessionHandler.HandleResult(context), null);
        }
        #endregion

        #region Events Listener

        protected abstract void EventsListener(object sender, DoWorkEventArgs e);

        protected bool HandlePacketEventsListener(string payload, PacketType packetType)
        {
            try
            {
                switch (packetType)
                {
                    case PacketType.AlgorithmStatus:
                        var algorithmStatusPacket = JsonConvert.DeserializeObject<AlgorithmStatusPacket>(payload, _jsonSettings);
                        _packetQueue.Add(algorithmStatusPacket);
                        break;
                    
                    case PacketType.AlgorithmNameUpdate:
                        var algorithmNameUpdatePacket = JsonConvert.DeserializeObject<AlgorithmNameUpdatePacket>(payload, _jsonSettings);
                        _algorithmId = algorithmNameUpdatePacket.AlgorithmId;
                        InitializeOrderEventJsonConverter();
                        _packetQueue.Add(algorithmNameUpdatePacket);
                        break;

                    case PacketType.LiveNode:
                        var liveNodePacket = JsonConvert.DeserializeObject<LiveNodePacket>(payload, _jsonSettings);
                        _packetQueue.Add(liveNodePacket);
                        break;

                    case PacketType.AlgorithmNode:
                        var algorithmNodePacket = JsonConvert.DeserializeObject<AlgorithmNodePacket>(payload, _jsonSettings);
                        _algorithmId = algorithmNodePacket.AlgorithmId;
                        InitializeOrderEventJsonConverter();
                        _packetQueue.Add(algorithmNodePacket);
                        break;

                    case PacketType.LiveResult:
                        var liveResultPacket = JsonConvert.DeserializeObject<LiveResultPacket>(payload, _jsonSettings);
                        _packetQueue.Add(liveResultPacket);
                        break;

                    case PacketType.BacktestResult:
                        var backtestResultPacket = JsonConvert.DeserializeObject<BacktestResultPacket>(payload, _jsonSettings);
                        _packetQueue.Add(backtestResultPacket);
                        break;

                    case PacketType.OrderEvent:
                        var orderEventPacket = JsonConvert.DeserializeObject<OrderEventPacket>(payload, _jsonSettings);
                        _packetQueue.Add(orderEventPacket);
                        break;

                    case PacketType.Log:
                        var logPacket = JsonConvert.DeserializeObject<LogPacket>(payload, _jsonSettings);
                        _packetQueue.Add(logPacket);
                        break;

                    case PacketType.Debug:
                        var debugPacket = JsonConvert.DeserializeObject<DebugPacket>(payload, _jsonSettings);
                        _packetQueue.Add(debugPacket);
                        break;

                    case PacketType.HandledError:
                        var handledErrorPacket = JsonConvert.DeserializeObject<HandledErrorPacket>(payload, _jsonSettings);
                        _packetQueue.Add(handledErrorPacket);
                        break;

                    default:
                        _logger.LogWarning("HandlePacketEventsListener: Unknown packet type '{PacketType}'.", packetType);
                        return false;
                }
                return true;
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("HandlePacketEventsListener: Queue is disposed.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HandlePacketEventsListener: Error deserializing packet of type '{PacketType}': {Payload}", packetType, payload/*.Substring(0, Math.Min(payload.Length, 100))*/);
                return false;
            }
        }

        private void InitializeOrderEventJsonConverter()
        {
            if (_orderEventJsonConverter == null && !string.IsNullOrEmpty(_algorithmId))
            {
                _orderEventJsonConverter = new OrderEventJsonConverter(_algorithmId);
                _jsonSettings.Converters.Add(_orderEventJsonConverter);
                _logger.LogDebug("Initialized OrderEventJsonConverter with AlgorithmId: {AlgorithmId}", _algorithmId);
            }
        }

        #endregion

        public virtual void Dispose()
        {
            _cts?.Dispose();
            _eternalQueueListener.Dispose();
            _queueReader.Dispose();
            _packetQueue.Dispose();
            GC.SuppressFinalize(this);
        }

        public bool CanSubscribe { get; } = true;

        private SessionState _state = SessionState.Unsubscribed;
        public SessionState State
        {
            get => _state;
            protected set
            {
                _state = value;
                _sessionHandler.HandleStateChanged(value);
            }
        }
    }
}
