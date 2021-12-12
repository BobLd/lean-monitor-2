using Panoptes.Model.Serialization.Packets;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PacketType = QuantConnect.Packets.PacketType;

namespace Panoptes.Model.Sessions.File
{
    public sealed class FileSession : ISession
    {
        private readonly IResultSerializer _resultSerializer;
        private readonly ISessionHandler _sessionHandler;
        private readonly SynchronizationContext _syncContext;

        private readonly bool _watchFile;

        public string Name { get; private set; }

        private FileSystemWatcher _watcher;
        private SessionState _state = SessionState.Unsubscribed;

        public FileSession(ISessionHandler resultHandler, IResultSerializer resultSerializer, FileSessionParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrWhiteSpace(parameters.FileName))
            {
                throw new ArgumentException("FileName is required.", nameof(parameters));
            }

            _watchFile = parameters.Watch;
            _syncContext = SynchronizationContext.Current;

            if (_syncContext == null)
            {
                throw new NullReferenceException($"FileSession: {SynchronizationContext.Current} is null, please make sure the seesion was created in UI thread.");
            }

            _resultSerializer = resultSerializer;
            _sessionHandler = resultHandler;

            Name = parameters.FileName;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            // Initially open the file
            await ReadFromFileAsync(cancellationToken).ConfigureAwait(false);

            // Return when we do not have to configure the file system watcher
            if (!_watchFile) return;

            // Open a monitoring sessionss
            Subscribe(); // async?
        }

        public void Initialize()
        {
            // Initially open the file
            ReadFromFile();

            // Return when we do not have to configure the file system watcher
            if (!_watchFile) return;

            // Open a monitoring sessionss
            Subscribe();
        }

        public void Shutdown()
        {
            Unsubscribe();
        }

        private async Task ReadFromFileAsync(CancellationToken cancellationToken)
        {
            if (!System.IO.File.Exists(Name))
            {
                throw new FileNotFoundException($"File '{Name}' does not exist");
            }

            await foreach (var logItem in _resultSerializer.GetBacktestLogs(Name, cancellationToken).ConfigureAwait(false))
            {
                _syncContext.Send(_ => _sessionHandler.HandleLogMessage(logItem.Item1, logItem.Item2, LogItemType.Monitor), null);
            }

            var result = await _resultSerializer.DeserializeAsync(Name, cancellationToken).ConfigureAwait(false);

            var context = new ResultContext
            {
                Name = Name,
                Result = result,
                Progress = 1
            };

            // Send order events
            result.OrderEvents?.ForEach(oe =>
            {
                _syncContext.Send(_ => _sessionHandler.HandleOrderEvent(new OrderEventPacket() { Event = oe, Type = PacketType.OrderEvent }), null);
            });

            // Send results
            _syncContext.Send(_ => _sessionHandler.HandleResult(context), null);
        }

        private void ReadFromFile()
        {
            if (!System.IO.File.Exists(Name))
            {
                throw new FileNotFoundException($"File '{Name}' does not exist");
            }

            var result = _resultSerializer.Deserialize(Name);

            var context = new ResultContext
            {
                Name = Name,
                Result = result,
                Progress = 1
            };

            // Send order events
            result.OrderEvents.ForEach(oe =>
            {
                _syncContext.Send(_ => _sessionHandler.HandleOrderEvent(new OrderEventPacket() { Event = oe, Type = PacketType.OrderEvent }), null);
            });

            // Send results
            _syncContext.Send(_ => _sessionHandler.HandleResult(context), null);
        }

        public void Subscribe()
        {
            State = SessionState.Subscribed;

            if (!Path.IsPathRooted(Name))
            {
                // Combine with the current directory to allow for the FileSystemWatcher to monitor
                Name = Path.Combine(Environment.CurrentDirectory, Name);
            }

            var directoryName = Path.GetDirectoryName(Name);
            if (directoryName != null)
            {
                _watcher = new FileSystemWatcher(directoryName)
                {
                    EnableRaisingEvents = _watchFile
                };
            }

            _watcher.Changed += (sender, args) =>
            {
                if (args.Name == Path.GetFileName(Name))
                {
                    _syncContext.Post(_ => ReadFromFile(), null);
                }
            };
        }

        public void Unsubscribe()
        {
            State = SessionState.Unsubscribed;

            if (_watcher == null)
            {
                // This file has no watcher session.
                return;
            }

            _watcher.EnableRaisingEvents = false;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        public SessionState State
        {
            get { return _state; }
            private set
            {
                _state = value;
                _sessionHandler.HandleStateChanged(value); // _syncContext??
            }
        }

        public bool CanSubscribe { get; } = true;
    }
}
