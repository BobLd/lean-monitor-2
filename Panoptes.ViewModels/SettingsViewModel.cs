using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Panoptes.Model.Settings;

namespace Panoptes.ViewModels
{
    public sealed class SettingsViewModel : ObservableRecipient
    {
        public readonly ISettingsManager SettingsManager;

        public SettingsViewModel(IMessenger messenger, ISettingsManager settingsManager) : base(messenger)
        {
            SettingsManager = settingsManager;

            //Messenger.Register<SettingsViewModel, ShowNewSessionWindowMessage>(this, async (r, _) => await r._settingsManager.InitialiseAsync().ConfigureAwait(false));
            //Messenger.Register<SettingsViewModel, SessionOpenedMessage>(this, async (r, _) => await r._settingsManager.InitialiseAsync().ConfigureAwait(false));
        }
    }
}
