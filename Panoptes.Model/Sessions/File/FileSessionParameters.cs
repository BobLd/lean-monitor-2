namespace Panoptes.Model.Sessions.File
{
    /// <summary>
    /// Parameters used for the FileSession
    /// </summary>
    public class FileSessionParameters : ISessionParameters
    {     /// <summary>
          /// The JSON fileName to open
          /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets whether the FileSession will monitor the file for changes.
        /// </summary>
        public bool Watch { get; set; }
    }
}
