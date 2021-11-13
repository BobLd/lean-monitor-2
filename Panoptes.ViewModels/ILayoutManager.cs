using Dock.Model;

namespace Panoptes.ViewModels
{
    public interface ILayoutManager
    {
        void LoadLayout(DockManager manager);
        void ResetLayout(DockManager manager);
        void SaveLayout(DockManager manager);
    }
}
