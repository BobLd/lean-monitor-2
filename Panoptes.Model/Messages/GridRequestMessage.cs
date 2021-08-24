using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;

namespace Panoptes.Model.Messages
{
    public class GridRequestMessage : ValueChangedMessage<string>
    {
        public GridRequestMessage(string key) : base(key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
        }

        public string Key { get; private set; }
    }
}
