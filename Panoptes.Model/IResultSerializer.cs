using System.Threading;
using System.Threading.Tasks;

namespace Panoptes.Model
{
    public interface IResultSerializer
    {
        Result Deserialize(string pathToResult);

        Task<Result> DeserializeAsync(string pathToResult, CancellationToken cancellationToken);

        string Serialize(Result result);
    }
}
