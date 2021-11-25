using Avalonia;

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
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (System.Exception ex)
            {
                System.IO.File.WriteAllText("ERROR.txt", ex.ToString());
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
