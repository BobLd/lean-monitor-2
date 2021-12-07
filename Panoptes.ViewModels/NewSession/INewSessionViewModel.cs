using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;

namespace Panoptes.ViewModels.NewSession
{
    public interface INewSessionViewModel
    {
        AsyncRelayCommand OpenCommandAsync { get; }

        RelayCommand CancelCommand { get; }

        string Header { get; }

        void LoadParameters(IDictionary<string, string> parameters);
    }
}
