using Panoptes.Model.Serialization.Packets;

namespace Panoptes.Model
{
    public interface IResultConverter
    {
        Result FromBacktestResult(BacktestResult backtestResult);
        Result FromLiveResult(LiveResult liveResult);

        BacktestResult ToBacktestResult(Result result);
        LiveResult ToLiveResult(Result result);
    }
}
