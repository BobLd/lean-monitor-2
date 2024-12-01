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
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                // Get the File Version
                var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                var fileVersion = fvi.FileVersion;

                // Alternatively, get the Assembly Version
                var assemblyVersion = assembly.GetName().Version.ToString();

                // Choose which version to return
                return fileVersion ?? assemblyVersion;
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

        /// <summary>
        /// Get the file size in MB. Returns <c>-1</c> if the file is not found.
        /// </summary>
        public static long GetFileSize(string path)
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length / 1_048_576;
            }
            return -1;
        }
    }
}
