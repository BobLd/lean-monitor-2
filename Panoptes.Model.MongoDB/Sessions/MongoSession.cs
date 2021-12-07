using MongoDB.Driver;
using Panoptes.Model.Serialization.Packets;
using Panoptes.Model.Sessions;
using Panoptes.Model.Sessions.Stream;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PacketType = QuantConnect.Packets.PacketType;

namespace Panoptes.Model.MongoDB.Sessions
{
    public sealed class MongoSession : BaseStreamSession, ISessionHistory
    {
        private MongoClient _client;
        private IMongoDatabase _database;
        private IMongoCollection<MongoDbPacket> _collection;

        public string DatabaseName { get; }
        public string CollectionName { get; }

        public MongoSession(ISessionHandler sessionHandler, IResultConverter resultConverter, MongoSessionParameters parameters)
            : base(sessionHandler, resultConverter, parameters)
        {
            // This happen in UI thread, do not put blocking code in here
            _client = new MongoClient(new MongoClientSettings
            {
                Credential = MongoCredential.CreateCredential(null, parameters.UserName, parameters.Password),
                Server = new MongoServerAddress(_host, _port),
                ApplicationName = $"{Global.AppName} {Global.AppVersion} {Global.MachineName} {Global.OSVersion}",
                HeartbeatInterval = TimeSpan.FromSeconds(5),
                HeartbeatTimeout = TimeSpan.FromSeconds(40)
            });

            DatabaseName = parameters.DatabaseName;
            CollectionName = parameters.CollectionName;
        }

        public override void Initialize()
        {
            // Load today's data?
            _database = _client.GetDatabase(DatabaseName); // backtest or live
            _collection = _database.GetCollection<MongoDbPacket>(CollectionName); // algo name / id

            base.Initialize();
        }

        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.WriteLine($"MongoSession.InitializeAsync: {_client.Settings}, DB: {DatabaseName}, Collection: {CollectionName}");
            _database = _client.GetDatabase(DatabaseName); // backtest or live
            _collection = _database.GetCollection<MongoDbPacket>(CollectionName); // algo name / id

            await base.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        private static bool CheckliveResultPacket(LiveResultPacket liveResultPacket)
        {
            bool cashOk = liveResultPacket.Results.Cash?.Count > 0;
            bool holdingsOk = liveResultPacket.Results.Holdings?.Count > 0;
            return cashOk && holdingsOk;
        }

        public async Task LoadRecentDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                Debug.WriteLine($"MongoSession.LoadRecentDataAsync: {_client.Settings}, DB: {DatabaseName}, Collection: {CollectionName}");
                var collection = _client.GetDatabase(DatabaseName).GetCollection<MongoDbPacket>(CollectionName); // backtest or live + algo name / id
                var builder = Builders<MongoDbPacket>.Filter;

                //https://stackoverflow.com/questions/8749971/can-i-query-mongodb-objectid-by-date
                //var dateFilter = builder.Gte(x => x.Id, new ObjectId(DateTime.UtcNow.AddDays(-1), 0, 0, 0)); // last 24h
                var sort = Builders<MongoDbPacket>.Sort.Descending("_id");

                // Live node LiveNode
                var liveNodeFilter = builder.Eq(x => x.Type, "LiveNode");
                using (IAsyncCursor<MongoDbPacket> cursor = await collection.FindAsync(liveNodeFilter, new FindOptions<MongoDbPacket> { BatchSize = 1, NoCursorTimeout = false, Sort = sort, Limit = 1 }, cancellationToken).ConfigureAwait(false))
                {
                    while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    {
                        foreach (var packet in cursor.Current)
                        {
                            HandlePacketEventsListener(packet.Message, Enum.Parse<PacketType>(packet.Type));
                        }
                        break;
                    }
                }

                // Status
                var statusFilter = builder.Eq(x => x.Type, "AlgorithmStatus");
                using (IAsyncCursor<MongoDbPacket> cursor = await collection.FindAsync(statusFilter, new FindOptions<MongoDbPacket> { BatchSize = 1, NoCursorTimeout = false, Sort = sort, Limit = 1 }, cancellationToken).ConfigureAwait(false))
                {
                    while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    {
                        foreach (var packet in cursor.Current)
                        {
                            HandlePacketEventsListener(packet.Message, Enum.Parse<PacketType>(packet.Type));
                        }
                        break;
                    }
                }

                // Live results
                var liveResultFilter = builder.Eq(x => x.Type, "LiveResult"); //builder.And(dateFilter, builder.Eq(x => x.Type, "LiveResult"));
                var options = new FindOptions<MongoDbPacket> { BatchSize = 5, NoCursorTimeout = false, Sort = sort, Limit = 100 };

                LiveResultPacket liveResultPacket = null;
                bool liveResultContinue = true;
                using (IAsyncCursor<MongoDbPacket> cursor = await collection.FindAsync(liveResultFilter, options, cancellationToken).ConfigureAwait(false))
                {
                    while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (!liveResultContinue) break;

                        foreach (var packet in cursor.Current)
                        {
                            if (liveResultPacket == null)
                            {
                                liveResultPacket = JsonSerializer.Deserialize<LiveResultPacket>(packet.Message, _options);
                                if (CheckliveResultPacket(liveResultPacket))
                                {
                                    liveResultContinue = false;
                                    break;
                                }
                            }
                            else
                            {
                                if (CheckliveResultPacket(liveResultPacket))
                                {
                                    liveResultContinue = false;
                                    break;
                                }
                                else
                                {
                                    var liveResultPacketLocal = JsonSerializer.Deserialize<LiveResultPacket>(packet.Message, _options);

                                    // Update cash
                                    if ((liveResultPacket.Results.Cash == null || liveResultPacket.Results.Cash.Count == 0) &&
                                        (liveResultPacketLocal.Results.Cash?.Count > 0))
                                    {
                                        liveResultPacket.Results.Cash = liveResultPacketLocal.Results.Cash;
                                    }

                                    //  Update holdings
                                    if ((liveResultPacket.Results.Holdings == null || liveResultPacket.Results.Holdings.Count == 0) &&
                                        (liveResultPacketLocal.Results.Holdings?.Count > 0))
                                    {
                                        liveResultPacket.Results.Holdings = liveResultPacketLocal.Results.Holdings;
                                    }
                                }

                                if (CheckliveResultPacket(liveResultPacket))
                                {
                                    liveResultContinue = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (liveResultFilter != null)
                {
                    _packetQueue.Add(liveResultPacket, cancellationToken);
                }
            }
            catch (TimeoutException toEx)
            {
                throw new TimeoutException("MongoSession.LoadRecentData: Session timed out.", toEx);
            }
            catch (MongoAuthenticationException authEx)
            {
                // wrong user/password
                throw new ArgumentOutOfRangeException("MongoSession.LoadRecentData: Wrong user/password", authEx);
            }
            catch (OperationCanceledException ocex)
            {
                // Cancel token requested
                throw new OperationCanceledException("MongoSession.LoadRecentData", ocex);
            }
            catch (Exception ex)
            {
                throw new Exception("MongoSession.LoadRecentData", ex);
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
            catch (TimeoutException toEx)
            {
                Unsubscribe();
                Debug.WriteLine($"MongoSession.EventsListener: Session timed out and proceeded with unsubscribing. {toEx}");
            }
            catch (MongoAuthenticationException authEx)
            {
                // wrong user/password
                throw new ArgumentOutOfRangeException("MongoSession.EventsListener: Wrong user/password.", authEx);
            }
            catch (OperationCanceledException ocex)
            {
                // Cancel token requested
                throw new OperationCanceledException($"MongoSession.EventsListener: {ocex.Message}", ocex);
            }
            catch (Exception ex)
            {
                throw new Exception($"MongoSession.EventsListener: {ex.Message}", ex);
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            // We need to dispose the cluster to make sure connections are closed
            _client?.Cluster.Dispose();

            _client = null;
            _collection = null;
            _database = null;
        }
    }
}
