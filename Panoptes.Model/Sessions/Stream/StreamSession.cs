using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using QuantConnect.Packets;
using System;
using System.ComponentModel;
using System.Text;

namespace Panoptes.Model.Sessions.Stream
{
    public sealed class StreamSession : BaseStreamSession
    {
        public StreamSession(ISessionHandler sessionHandler, IResultConverter resultConverter,
            StreamSessionParameters parameters, ILogger logger)
           : base(sessionHandler, resultConverter, parameters, logger)
        {

        }

        private readonly TimeSpan _timeOut = TimeSpan.FromMilliseconds(500);

        protected override void EventsListener(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (var pullSocket = new PullSocket($">tcp://{_host}:{_port}"))
                {
                    _logger.LogInformation("EventsListener: Connected to {Host}:{Port}", _host, _port);

                    while (!_eternalQueueListener.CancellationPending && !_cts.Token.IsCancellationRequested)
                    {
                        var message = new NetMQMessage();
                        if (!pullSocket.TryReceiveMultipartMessage(_timeOut, ref message))
                        {
                            continue;
                        }

                        if (message.FrameCount != 1)
                        {
                            _logger.LogWarning("EventsListener: Received message with unexpected FrameCount {FrameCount}", message.FrameCount);
                            continue;
                        }
                        // There should only be 1 part messages
                        var payload = message[0].ConvertToString(Encoding.UTF8);

                        _logger.LogDebug("EventsListener: Received payload: {Payload}", payload);

                        Packet packet;
                        try
                        {
                            packet = JsonConvert.DeserializeObject<Packet>(payload, _jsonSettings);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "EventsListener: Error deserializing Packet.");
                            continue;
                        }

                        if (packet == null)
                        {
                            _logger.LogWarning("EventsListener: Deserialized packet is null.");
                            continue;
                        }

                        _logger.LogDebug("EventsListener: Deserialized packet of type {PacketType}", packet.Type);

                        HandlePacketEventsListener(payload, packet.Type);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EventsListener: Exception occurred.");
                throw;
            }
            finally
            {
                _resetEvent.Set();
            }
        }
    }
}

