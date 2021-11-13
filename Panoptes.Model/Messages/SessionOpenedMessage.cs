using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public sealed class SessionOpenedMessage : ValueChangedMessage<bool>
    {
        public bool IsSuccess => Value;

        public string Error { get; }

        /// <summary>
        /// Session opened successfully.
        /// </summary>
        public SessionOpenedMessage() : base(true)
        { }

        /// <summary>
        /// Session opening failed.
        /// </summary>
        /// <param name="error"></param>
        public SessionOpenedMessage(string error) : base(false)
        {
            Error = error;
        }
    }
}
