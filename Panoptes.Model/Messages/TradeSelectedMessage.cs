using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Panoptes.Model.Messages
{
    public sealed class TradeSelectedMessage : ValueChangedMessage<int[]>
    {
        public string Sender { get; }

        /// <summary>
        /// Is it a cumulative selection (e.i. Ctrl is press down).
        /// </summary>
        public bool IsCumulative { get; }

        public TradeSelectedMessage(string sender, int[] ids, bool isCumulative) : base(ids)
        {
            Sender = sender;
            IsCumulative = isCumulative;
        }
    }
}
