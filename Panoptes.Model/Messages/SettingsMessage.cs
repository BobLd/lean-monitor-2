using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Panoptes.Model.Settings;

namespace Panoptes.Model.Messages
{
    public sealed class SettingsMessage : ValueChangedMessage<UserSettings>
    {
        public UserSettingsUpdate Type { get; }

        public SettingsMessage(UserSettings value, UserSettingsUpdate type) : base(value)
        {
            Type = type;
        }
    }

    public enum UserSettingsUpdate
    {
        Timezone = 0,

    }
}