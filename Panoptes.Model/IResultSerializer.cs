namespace Panoptes.Model
{
    public interface IResultSerializer
    {
        Result Deserialize(string pathToResult);

        string Serialize(Result result);
    }
}
