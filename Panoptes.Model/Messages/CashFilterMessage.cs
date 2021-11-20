using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System.Threading;

namespace Panoptes.Model.Messages
{
    public sealed class CashFilterMessage : ValueChangedMessage<string>
    {
        public CashFilterMessage(string source, string search, CancellationToken ct) : base(source)
        {
            Search = search;
            CancellationToken = ct;
        }

        public string Source => base.Value;

        public string Search { get; }

        public CancellationToken CancellationToken { get; }
    }
}
