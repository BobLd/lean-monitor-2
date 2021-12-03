using System;
using System.Diagnostics;
using System.IO;

namespace Panoptes.Model
{
    public static class Global
    {
        public const string AppName = "Panoptes";

        /// <summary>
        /// Returns the path of the executable that started the currently executing process.
        /// Returns null when the path is not available.
        /// </summary>
        public static string ProcessPath => Environment.ProcessPath;

        /// <summary>
        /// Returns the directory of the executable that started the currently executing process.
        /// Returns null when the path is not available.
        /// </summary>
        public static string ProcessDirectory => Path.GetDirectoryName(ProcessPath);

        /// <summary>
        /// Gets the NetBIOS name of this local computer.
        /// </summary>
        public static string MachineName => Environment.MachineName;

        /// <summary>
        /// Gets the current platform identifier and version number.
        /// </summary>
        public static string OSVersion => Environment.OSVersion.VersionString;

        private static string _appVersion;
        public static string AppVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_appVersion))
                {
                    _appVersion = GetVersion();
                }
                return _appVersion;
            }
        }

        private static string GetVersion()
        {
            try
            {
                // https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file
                var fvi = FileVersionInfo.GetVersionInfo(Environment.ProcessPath);

#pragma warning disable CS8603 // Possible null reference return.
                if (fvi != null)
                {
                    if (fvi.FileVersion == fvi.ProductVersion)
                    {
                        return fvi.FileVersion;
                    }
                    else
                    {
                        return $"{fvi.FileVersion} ({fvi.ProductVersion})";
                    }
                }
                return null;
#pragma warning restore CS8603 // Possible null reference return.
            }
            catch (Exception e)
            {
                return $"ERROR in version: {e.Message}";
            }
        }

        public static Version ParseVersion(string VersionStr)
        {
            if (Version.TryParse(VersionStr, out var version))
            {
                return version;
            }

            if (VersionStr.Contains(' '))
            {
                if (Version.TryParse(VersionStr.Split(' ')[0], out version))
                {
                    return version;
                }
                throw new ArgumentException();
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}
