using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Panoptes.Model.MongoDB
{
    public class MongoDbPacket
    {
        public ObjectId Id { get; set; }

        [BsonRequired]
        public string Channel { get; internal set; }

        [BsonRequired]
        public string Type { get; internal set; }

        [BsonRequired]
        public string Message { get; internal set; }
    }
}
