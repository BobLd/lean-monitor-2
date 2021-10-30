using MongoDB.Bson;
using MongoDB.Driver;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.Stream;
using QuantConnect.Packets;
using System;
using System.ComponentModel;

namespace Panoptes.Model.MongoDB.Sessions
{
    public sealed class MongoSession : BaseStreamSession
    {
        private readonly MongoClient _client;
        private IMongoDatabase _database;
        private IMongoCollection<MongoDbPacket> _collection;

        public MongoSession(ISessionHandler sessionHandler, IResultConverter resultConverter, MongoSessionParameters parameters)
            : base(sessionHandler, resultConverter, parameters)
        {
            _client = new MongoClient(new MongoClientSettings
            {
                Credential = MongoCredential.CreateCredential(null, parameters.UserName, parameters.Password),
                Server = new MongoServerAddress(_host, _port)
            });

            try
            {
                // Check if password is correct
                var names = _client.ListDatabaseNames();
            }
            catch (TimeoutException toEx)
            {
                // timeout
                throw;
            }
            catch (MongoAuthenticationException authEx)
            {
                // wrong user/password
                throw;
            }
        }

        public override void Initialize()
        {
            // Load today's data?
            _database = _client.GetDatabase("backtest-test"); // backtest or live
            _collection = _database.GetCollection<MongoDbPacket>("bar-3"); // algo name / id

            //LoadPreviousData();
            base.Initialize();
        }

        private void LoadPreviousData()
        {
            try
            {
                //https://stackoverflow.com/questions/8749971/can-i-query-mongodb-objectid-by-date
                //var hexSeconds = ((long)Math.Floor((double)DateTimeOffset.UtcNow.ToUnixTimeSeconds())).ToString("X");
                //var constructedObjectId = new ObjectId(hexSeconds + "0000000000000000");
                var constructedObjectId = new ObjectId(DateTime.Today.AddDays(-1), 0, 0, 0);

                var builder = Builders<MongoDbPacket>.Filter;
                var dateFilter = builder.Gte(x => x.Id, constructedObjectId);

                // Status
                var statusFilter = builder.And(dateFilter, builder.Eq(x => x.Type, "AlgorithmStatus"));
                var first = _collection.Find(statusFilter).SortByDescending(x => x.Id).FirstOrDefault();
                if (first != null)
                {
                    HandlePacketEventsListener(first.Message, Enum.Parse<PacketType>(first.Type));
                }

                var liveResultFilter = builder.And(dateFilter, builder.Eq(x => x.Type, "LiveResult"));
                foreach (var mongoPacket in _collection.Find(liveResultFilter).SortByDescending(x => x.Id)
                    .Limit(40).SortBy(x => x.Id).ToEnumerable()) //_cts.Token))
                {
                    HandlePacketEventsListener(mongoPacket.Message, Enum.Parse<PacketType>(mongoPacket.Type));
                }
            }
            catch (MongoAuthenticationException authEx)
            {
                // wrong user/password
            }
            catch (OperationCanceledException)
            {
                // Cancel token requested
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override void EventsListener(object sender, DoWorkEventArgs e)
        {
            // https://mongodb.github.io/mongo-csharp-driver/2.9/reference/driver/change_streams/
            // https://docs.mongodb.com/manual/tutorial/convert-standalone-to-replica-set/
            // https://adelachao.medium.com/create-a-mongodb-replica-set-in-windows-edeab1c85894

            try
            {
                // Watching changes in a single collection
                using (var cursor = _collection.Watch(cancellationToken: _cts.Token))
                {
                    // Connection succesfull here

                    foreach (var doc in cursor.ToEnumerable(_cts.Token))
                    {
                        if (_eternalQueueListener.CancellationPending) break;
                        HandlePacketEventsListener(doc.FullDocument.Message, Enum.Parse<PacketType>(doc.FullDocument.Type));
                    }
                }
            }
            catch (MongoAuthenticationException authEx)
            {
                // wrong user/password
            }
            catch (OperationCanceledException)
            {
                // Cancel token requested
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
