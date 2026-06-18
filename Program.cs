using Avalonia;
using Velopack;

namespace Sabeltann;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack must run before anything else so it can handle install,
        // update and uninstall hooks invoked by the updater. On a normal
        // launch this returns immediately.
        VelopackApp.Build().Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
