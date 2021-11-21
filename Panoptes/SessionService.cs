using Avalonia.Threading;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using Panoptes.Model.MongoDB.Sessions;
using Panoptes.Model.Serialization.Packets;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.File;
using Panoptes.Model.Sessions.Stream;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Panoptes
{
    internal sealed class SessionService : ISessionService, ISessionHandler
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly Timer _timer;
#pragma warning restore IDE0052 // Remove unread private members

        private readonly IMessenger _messenger;
        private readonly IResultConverter _resultConverter;
        private readonly IResultSerializer _resultSerializer;
        private readonly IResultMutator _resultMutator;
        //private readonly IApiClient _apiClient;

        private ISession _session;

        public Result LastResult { get; private set; }

        public bool IsSessionActive => _session != null;

        public SessionService(IMessenger messenger, IResultConverter resultConverter,
            IResultSerializer resultSerializer, IResultMutator resultMutator)
        {
            _messenger = messenger;
            _resultConverter = resultConverter;
            _resultSerializer = resultSerializer;
            _resultMutator = resultMutator;

            // Need to check if it's a live session, or put it in Open(ISessionParameters parameters)
            // At a later stage, timer should be done on server side
            bool isLive = true;
            if (isLive)
            {
                _timer = new Timer(OnMinute, null,
                    TimeSpan.FromSeconds(Times.GetSecondsToNextMinute() + Times.OneMillisecond.TotalSeconds),
                    Times.OneMinute);
            }
        }

        private void OnMinute(object? state)
        {
#if DEBUG
            Debug.WriteLine($"GC.GetTotalMemory={GC.GetTotalMemory(false) / 1048576:0.0}MB");
#endif

            var utcNow = DateTime.UtcNow;

            // Check if new day
            if (utcNow.TimeOfDay.TotalSeconds < 60)
            {
                // New year
                if (utcNow.DayOfYear == 1)
                {
                    _messenger.Send(new TimerMessage(TimerMessage.TimerEventType.NewYear, utcNow));
                }

                // New month
                if (utcNow.Day == 1)
                {
                    _messenger.Send(new TimerMessage(TimerMessage.TimerEventType.NewMonth, utcNow));
                }

                // New week
                if (utcNow.DayOfWeek == DayOfWeek.Monday)
                {
                    _messenger.Send(new TimerMessage(TimerMessage.TimerEventType.NewWeek, utcNow));
                }

                // New day
                _messenger.Send(new TimerMessage(TimerMessage.TimerEventType.NewDay, utcNow));
            }

            // New hour
            if (utcNow.TimeOfDay.TotalHours % utcNow.TimeOfDay.Hours < Times.OneMinute.TotalHours)
            {
                _messenger.Send(new TimerMessage(TimerMessage.TimerEventType.NewHour, utcNow));
            }

            // New minute
            _messenger.Send(new TimerMessage(TimerMessage.TimerEventType.NewMinute, utcNow));
        }

        public void HandleResult(ResultContext resultContext)
        {
            if (resultContext == null)
            {
                throw new ArgumentNullException(nameof(resultContext));
            }

            if (_session == null)
            {
                // It might be the case, result has still been sent from an old worker thread before the worker got cancelled.
                // TODO: It might be interesting to use unique ID's for the session
                // so if a new session has been opened meanwhile, we do process new results
                return;
            }

            // Update the last result
            LastResult = resultContext.Result;

            // Apply mutations
            _resultMutator.Mutate(resultContext.Result);

            // Send a message indicating the session has been updated
            _messenger.Send(new SessionUpdateMessage(resultContext));
        }

        public void HandleLogMessage(string message, LogItemType type)
        {
            // Live log message, use current DateTime

            Debug.Assert(!string.IsNullOrEmpty(message));

            _messenger.Send(new LogEntryReceivedMessage(DateTime.Now, message, type));
        }

        public void HandleStateChanged(SessionState state)
        {
            _messenger.Send(new SessionStateChangedMessage(state));
        }

        public void HandleOrderEvent(OrderEventPacket orderEvent)
        {
            _messenger.Send(new OrderEventMessage(orderEvent));
        }

        public void HandleAlgorithmStatus(AlgorithmStatusPacket algorithmStatusPacket)
        {
            _messenger.Send(new AlgorithmStatusMessage(algorithmStatusPacket));
        }

        public void Initialize()
        {
            // We try to load instructions to load a session from the commandline.
            // This format is a bit obscure because it tries to say compatible with the 'port only'
            // argument as used in the Lean project.

            try
            {
                var arguments = Environment.GetCommandLineArgs();
                var argument = arguments.Last();

                // First try whether it is a port
                /*
                if (int.TryParse(argument, out int port))
                {
                    Open(new StreamSessionParameters
                    {
                        Host = "localhost",
                        Port = port
                    });
                    return;
                }
                */

                /*
                if (argument.EndsWith(".json"))
                {
                    // Expect it is a fileName
                    OpenFile(new FileSessionParameters
                    {
                        FileName = argument,
                        Watch = true
                    });
                    return;
                }
                */
            }
            catch (Exception ex)
            {
                // We were unable to open a session
                throw new Exception($"Invalid command line parameters: {Environment.GetCommandLineArgs()}", ex);
            }

            // Request a session by default
            _messenger.Send(new ShowNewSessionWindowMessage());
        }

        public void ShutdownSession()
        {
            if (_session == null)
            {
                Debug.WriteLine("SessionService.ShutdownSession: Cannot shutdown session. No session exists");
                return;
            }

            try
            {
                _session.Shutdown();
            }
            catch (Exception e)
            {
                throw new Exception("SessionService.ShutdownSession: Could not close the session", e);
            }
            finally
            {
                _session = null;
                LastResult = null;
                _messenger.Send(new SessionClosedMessage());
            }
        }

        public async Task Open(ISessionParameters parameters, CancellationToken cancellationToken)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            if (_session != null)
            {
                // Another session is open.
                // Close the session first before opening this new one
                ShutdownSession();
            }

            ISession session = null;

            // We need to make sure to create the session in UI thread. If not, SynchronizationContext.Current will be null
            // Because Open() is now call async, it's not the case anymore
            // https://stackoverflow.com/questions/7075491/why-is-synchronizationcontext-current-null

            if (parameters is MongoSessionParameters mongoParameters)
            {
                if (string.IsNullOrWhiteSpace(mongoParameters.Host))
                {
                    throw new ArgumentException("SessionService.Open: Host is required", nameof(parameters));
                }

                // Open a new session and open it
                await Dispatcher.UIThread.InvokeAsync(() => session = new MongoSession(this, _resultConverter, mongoParameters)).ConfigureAwait(false);
            }
            else if (parameters is StreamSessionParameters streamParameters)
            {
                if (string.IsNullOrWhiteSpace(streamParameters.Host))
                {
                    throw new ArgumentException("SessionService.Open: Host is required", nameof(parameters));
                }

                // Open a new session and open it
#if DEBUG
                var mockMessageHandler = new Model.Mock.MockStreamingMessageHandler(streamParameters);
                Task.Run(() => mockMessageHandler.Initialize(), cancellationToken);
#endif
                await Dispatcher.UIThread.InvokeAsync(() => session = new StreamSession(this, _resultConverter, streamParameters)).ConfigureAwait(false);
            }
            else if (parameters is FileSessionParameters fileParameters)
            {
                await Dispatcher.UIThread.InvokeAsync(() => session = new FileSession(this, _resultSerializer, fileParameters)).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException($"SessionService.Open: Unknown ISessionParameters of type '{parameters.GetType()}'.", nameof(parameters));
            }

            if (session == null)
            {
                throw new NullReferenceException("SessionService.Open: current session is null.");
            }

            await OpenSession(session, cancellationToken).ConfigureAwait(false);
        }

        /*
        public void OpenApi(ApiSessionParameters parameters)
        {
            if (_session != null)
            {
                // Another session is open.
                // Close the session first before opening this new one
                ShutdownSession();
            }

            var session = new ApiSession(this, _apiClient, parameters);
            OpenSession(session);
        }
        */

        private async Task OpenSession(ISession session, CancellationToken cancellationToken)
        {
            try
            {
                _session = session;
                if (session is ISessionHistory sessionHistory)
                {
                    await sessionHistory.LoadRecentData().ConfigureAwait(false);
                }

                await _session.InitializeAsync(cancellationToken).ConfigureAwait(false);
                // Notify the app of the new session
                _messenger.Send(new SessionOpenedMessage());
            }
            catch (OperationCanceledException ocEx)
            {
                Debug.WriteLine("SessionService.OpenSession: Operation canceled.");
            }
            catch (Exception e)
            {
                // Need log
                Debug.WriteLine(e);
                _messenger.Send(new SessionOpenedMessage(e.ToString()));
                throw new Exception("SessionService.OpenSession: Exception occured while opening the session", e);
            }
        }

        public bool IsSessionSubscribed
        {
            get
            {
                return _session?.State == SessionState.Subscribed;
            }
            set
            {
                if (_session.State == (value ? SessionState.Subscribed : SessionState.Unsubscribed)) return;
                if (_session.State == SessionState.Unsubscribed)
                {
                    _session.Subscribe();
                }
                else
                {
                    _session.Unsubscribe();
                }
            }
        }

        public bool CanSubscribe => _session?.CanSubscribe == true;
    }
}
