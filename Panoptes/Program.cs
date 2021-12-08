using Avalonia;
using Panoptes.Model;
using Serilog;
using System;
using System.IO;

namespace Panoptes
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            try
            {
                string logFilePath = Path.Combine(Global.ProcessDirectory, $"{DateTime.Today:ddMMyyyy}.log");
                Log.Logger = new LoggerConfiguration().WriteTo.Async(a => a.File(logFilePath))
#if DEBUG
                .WriteTo.Async(a => a.Debug())
                .MinimumLevel.Verbose()
#endif
                .CreateLogger();

                Log.Information("Starting {AppName} v{AppVersion} on '{OSVersion}' with args: {args}.", Global.AppName, Global.AppVersion, Global.OSVersion, args);

                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Program.Main({args})", args);
            }
            finally
            {
                Log.Information("Application closed.");
                Log.CloseAndFlush();
            }
        }

        // Might be optimised https://github.com/ahopper/Avalonia.IconPacks/blob/no-avalonia-desktop/src/Avalonia.IconPacks/Program.cs
        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
