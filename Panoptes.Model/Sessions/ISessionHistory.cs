using System.Threading.Tasks;

namespace Panoptes.Model.Sessions
{
    public interface ISessionHistory
    {
        Task LoadRecentData();
    }
}
