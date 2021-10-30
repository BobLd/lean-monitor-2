using Microsoft.Toolkit.Mvvm.Input;

namespace Panoptes.ViewModels.NewSession
{
    public interface INewSessionViewModel
    {
        AsyncRelayCommand OpenCommandAsync { get; }

        string Header { get; }
    }
}
