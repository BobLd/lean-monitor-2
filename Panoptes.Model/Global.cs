using System.Diagnostics;
using System.Reflection;

namespace Panoptes.Model
{
    public static class Global
    {
        public const string AppName = "Panoptes";

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
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
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
    }
}
