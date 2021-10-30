using Microsoft.Toolkit.Mvvm.Input;

namespace Panoptes.ViewModels.NewSession
{
    public interface INewSessionViewModel
    {
        AsyncRelayCommand OpenCommandAsync { get; }

        RelayCommand CancelCommand { get; }

        string Header { get; }
    }
}
