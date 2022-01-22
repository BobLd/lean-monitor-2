using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using Panoptes.Model.MongoDB.Sessions;
using Panoptes.Model.Serialization.Packets;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.File;
using Panoptes.Model.Sessions.Stream;
using Panoptes.Model.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        private readonly ISettingsManager _settingsManager;
        //private readonly IApiClient _apiClient;

        private  readonly ILogger _logger;

        private ISession _session;

        public Result LastResult { get; private set; }

        public bool IsSessionActive => _session != null;

        public SessionService(IMessenger messenger, IResultConverter resultConverter,
            IResultSerializer resultSerializer, IResultMutator resultMutator,
            ISettingsManager settingsManager, ILogger<SessionService> logger)
        {
            _logger = logger;
            _messenger = messenger;
            _resultConverter = resultConverter;
            _resultSerializer = resultSerializer;
            _resultMutator = resultMutator;
            _settingsManager = settingsManager;

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
            _logger.LogDebug("OnMinute: GC.GetTotalMemory={0:0.0}MB", GC.GetTotalMemory(false) / 1048576);
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

        public void HandleLogMessage(DateTime utcTime, string message, LogItemType type)
        {
            Debug.Assert(!string.IsNullOrEmpty(message));
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentOutOfRangeException(nameof(utcTime), $"The provided time should be in UTC time, received '{utcTime.Kind}'.");
            }

            _messenger.Send(new LogEntryReceivedMessage(utcTime, message, type));
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

        public void HandleLiveNode(LiveNodePacket liveNodePacket)
        {
            _messenger.Send(new LiveNodeMessage(liveNodePacket));
        }

        public async void Initialize()
        {
            // TODO - Not good at all, we need async here but it returns void!!

            // We try to load instructions to load a session from the commandline.
            // This format is a bit obscure because it tries to say compatible with the 'port only'
            // argument as used in the Lean project.

            try
            {
                var arguments = Environment.GetCommandLineArgs();

                var argument = arguments.Last();

                if (argument.EndsWith(".json") && File.Exists(argument))
                {
                    new Views.Windows.OpenBacktestWindow()
                    {
                        LoadingContent = $"{argument} ({Global.GetFileSize(argument):0.#} MB)",
                    }.Show();

                    await OpenAsync(new FileSessionParameters
                    {
                        FileName = argument,
                        Watch = false,
                        IsFromCmdLine = true
                    }, CancellationToken.None).ConfigureAwait(false);
                    return;
                }
                else if (argument.EndsWith(".qcb") && File.Exists(argument))
                {
                    new Views.Windows.OpenBacktestWindow()
                    {
                        LoadingContent = $"{argument} ({Global.GetFileSize(argument):0.#} MB)",
                    }.Show();

                    await OpenAsync(new FileSessionParameters
                    {
                        FileName = argument,
                        Watch = false,
                        IsFromCmdLine = true
                    }, CancellationToken.None).ConfigureAwait(false);
                    return;
                }

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
                _logger.LogWarning("SessionService.ShutdownSession: Cannot shutdown session, no session exists.");
                return;
            }

            try
            {
                _session.Shutdown();
                _session.Dispose();
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

        public async Task OpenAsync(ISessionParameters parameters, CancellationToken cancellationToken)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            if (_session != null)
            {
                // Another session is open.
                // Close the session first before opening this new one
                ShutdownSession();
            }

            if (!parameters.IsFromCmdLine)
            {
                // Save session parameters in settings
                _settingsManager.UpdateSessionParameters(parameters);
            }

            ISession session = null;

            // We need to make sure to create the session in UI thread. If not, SynchronizationContext.Current will be null
            // Because Open() is now call async, it's not the case anymore
            // https://stackoverflow.com/questions/7075491/why-is-synchronizationcontext-current-null

            if (parameters is MongoSessionParameters mongoParameters)
            {
                if (string.IsNullOrWhiteSpace(mongoParameters.Host))
                {
                    throw new ArgumentException("SessionService.Open: Host is required.", nameof(parameters));
                }

                // Open a new session and open it
                await Dispatcher.UIThread.InvokeAsync(() => session = new MongoSession(this, _resultConverter, mongoParameters, _logger)).ConfigureAwait(false);
            }
            else if (parameters is StreamSessionParameters streamParameters)
            {
                if (string.IsNullOrWhiteSpace(streamParameters.Host))
                {
                    throw new ArgumentException("SessionService.Open: Host is required.", nameof(parameters));
                }

                // Open a new session and open it
#if DEBUG
                var mockMessageHandler = new Model.Mock.MockStreamingMessageHandler(streamParameters);
                Task.Run(() => mockMessageHandler.Initialize(), cancellationToken);
#endif
                await Dispatcher.UIThread.InvokeAsync(() => session = new StreamSession(this, _resultConverter, streamParameters, _logger)).ConfigureAwait(false);
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

            await OpenSessionAsync(session, cancellationToken).ConfigureAwait(false);
        }

        private async Task OpenSessionAsync(ISession session, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("SessionService.OpenSessionAsync: {session}.", session);
                _session = session;
                if (session is ISessionHistory sessionHistory)
                {
                    _logger.LogInformation("SessionService.OpenSessionAsync: LoadRecentDataAsync for {session}.", session);
                    await sessionHistory.LoadRecentDataAsync(cancellationToken).ConfigureAwait(false);
                }

                await _session.InitializeAsync(cancellationToken).ConfigureAwait(false);
                // Notify the app of the new session
                _messenger.Send(new SessionOpenedMessage());
            }
            catch (OperationCanceledException ocEx)
            {
                _logger.LogDebug("SessionService.OpenSessionAsync: {session} was canceled.", session);
                _messenger.Send(new SessionOpenedMessage(ocEx.ToString()));
                ShutdownSession();
                throw new OperationCanceledException("SessionService.OpenSessionAsync: Operation canceled while opening the session.", ocEx);
            }
            catch (TimeoutException toEx)
            {
                _logger.LogError(toEx, "SessionService.OpenSessionAsync: TimeoutException with {session}", session);
                _messenger.Send(new SessionOpenedMessage(toEx.ToString()));
                ShutdownSession();
                throw new TimeoutException("SessionService.OpenSessionAsync: Operation timeout while opening the session.", toEx);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "SessionService.OpenSessionAsync: JsonException with {session}", session);
                _messenger.Send(new SessionOpenedMessage(jsonEx.ToString()));
                ShutdownSession();
                // We don't throw here, it's supposed to be handled now...
                //throw new Exception("SessionService.OpenSession: Exception occured while opening the session.", e);
            }
            catch (Exception e)
            {
                // Need log
                _logger.LogError(e, "SessionService.OpenSessionAsync: Exception with {session}", session);
                _messenger.Send(new SessionOpenedMessage(e.ToString()));
                ShutdownSession();
                throw new Exception("SessionService.OpenSession: Exception occured while opening the session.", e);
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
