using Panoptes.Model.Sessions.Stream;
using System.Security;

namespace Panoptes.Model.MongoDB.Sessions
{
    public class MongoSessionParameters : StreamSessionParameters
    {
        public string UserName { get; set; }

        public SecureString Password { get; set; }
    }
}
