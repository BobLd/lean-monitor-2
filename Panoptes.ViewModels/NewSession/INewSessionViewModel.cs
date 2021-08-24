using Microsoft.Toolkit.Mvvm.Input;

namespace Panoptes.ViewModels.NewSession
{
    public interface INewSessionViewModel
    {
        RelayCommand OpenCommand { get; }

        string Header { get; }
    }
}
