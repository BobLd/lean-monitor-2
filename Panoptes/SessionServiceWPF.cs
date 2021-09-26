using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model;
using Panoptes.Model.Messages;
using Panoptes.Model.Mock.Sessions;
using Panoptes.Model.MongoDB.Sessions;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.File;
using Panoptes.Model.Sessions.Stream;
using QuantConnect.Packets;
using System;
using System.Linq;

namespace Panoptes
{
    public sealed class SessionServiceWPF : ISessionService, ISessionHandler
    {
        private readonly IMessenger _messenger;
        private readonly IResultConverter _resultConverter;
        private readonly IResultSerializer _resultSerializer;
        private readonly IResultMutator _resultMutator;
        //private readonly IApiClient _apiClient;

        private ISession _session;

        public Result LastResult { get; private set; }

        public bool IsSessionActive => _session != null;

        public SessionServiceWPF(IMessenger messenger, IResultConverter resultConverter,
            IResultSerializer resultSerializer, IResultMutator resultMutator)
        {
            _messenger = messenger;
            _resultConverter = resultConverter;
            _resultSerializer = resultSerializer;
            _resultMutator = resultMutator;
        }

        public void HandleResult(ResultContext resultContext)
        {
            if (resultContext == null) throw new ArgumentNullException(nameof(resultContext));

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
                if (int.TryParse(argument, out int port))
                {
                    Open(new StreamSessionParameters
                    {
                        Host = "localhost",
                        Port = port
                    });
                    return;
                }

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
            if (_session == null) throw new Exception("No session exists");

            try
            {
                _session.Shutdown();
            }
            catch (Exception e)
            {
                throw new Exception("Could not close the session", e);
            }
            finally
            {
                _session = null;
                LastResult = null;
                _messenger.Send(new SessionClosedMessage());
            }
        }

        public void Open(ISessionParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            if (_session != null)
            {
                // Another session is open.
                // Close the session first before opening this new one
                ShutdownSession();
            }

            if (parameters is MongoSessionParameters mongoParameters)
            {
                if (string.IsNullOrWhiteSpace(mongoParameters.Host))
                {
                    throw new ArgumentException("Host is required", nameof(parameters));
                }

                // Open a new session and open it
                var session = new MongoSession(this, _resultConverter, mongoParameters);
                OpenSession(session);
            }
            else if (parameters is StreamSessionParameters streamParameters)
            {
                if (string.IsNullOrWhiteSpace(streamParameters.Host))
                {
                    throw new ArgumentException("Host is required", nameof(parameters));
                }

                // Open a new session and open it
#if DEBUG
                var session = new MockStreamSession(this, _resultConverter, streamParameters);
#else
                var session = new StreamSession(this, _resultConverter, streamParameters);
#endif
                OpenSession(session);
            }
            else if (parameters is FileSessionParameters fileParameters)
            {
                var session = new FileSession(this, _resultSerializer, fileParameters);
                OpenSession(session);
            }
            else
            {
                throw new ArgumentException($"Unknown ISessionParameters of type '{parameters.GetType()}'.", nameof(parameters));
            }
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

        private void OpenSession(ISession session)
        {
            try
            {
                _session = session;
                session.Initialize();
            }
            catch (Exception e)
            {
                throw new Exception("Exception occured while opening the session", e);
            }
            finally
            {
                // Notify the app of the new session
                _messenger.Send(new SessionOpenedMessage()); // should be in the 'try'
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
