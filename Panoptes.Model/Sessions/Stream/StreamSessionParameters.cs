namespace Panoptes.Model.Sessions.Stream
{
    public class StreamSessionParameters : ISessionParameters
    {
        public string Host { get; set; }

        public string Port { get; set; }

        /// <summary>
        /// Gets or sets whether this session should close after the last packet has been received.
        /// When disabled, New packets will reset the session state.
        /// </summary>
        public bool CloseAfterCompleted { get; set; } = true;

        public bool IsFromCmdLine { get; init; }
    }
}
