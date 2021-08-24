﻿using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using QuantConnect.Packets;
using System;
using System.ComponentModel;

namespace Panoptes.Model.Sessions.Stream
{
    public class StreamSession : BaseStreamSession
    {
        public StreamSession(ISessionHandler sessionHandler, IResultConverter resultConverter, StreamSessionParameters parameters)
           : base(sessionHandler, resultConverter, parameters)
        { }

        protected override void EventsListener(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (var pullSocket = new PullSocket($">tcp://{_host}:{_port}"))
                {
                    while (!_eternalQueueListener.CancellationPending)
                    {
                        var message = new NetMQMessage();
                        if (!pullSocket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(500), ref message))
                        {
                            continue;
                        }

                        // There should only be 1 part messages
                        if (message.FrameCount != 1) continue;

                        var payload = message[0].ConvertToString();
                        var packet = JsonConvert.DeserializeObject<Packet>(payload);

                        HandlePacketEventsListener(payload, packet.Type);
                    }

                    pullSocket.Close();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _resetEvent.Set();
            }
        }
    }
}

