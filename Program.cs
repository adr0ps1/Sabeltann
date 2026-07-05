using Avalonia;
using Velopack;
using Sabeltann.Services;

namespace Sabeltann;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Log otherwise-silent crashes so field issues leave a trace in logs/.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogService.Error("Unhandled exception", new { ex = (e.ExceptionObject as Exception)?.ToString() });

        // Velopack must run before anything else so it can handle install,
        // update and uninstall hooks invoked by the updater. On a normal
        // launch this returns immediately.
        VelopackApp.Build().Run();

        // Code past here runs only on a normal launch (install/update hooks exit above), so the
        // uninstall registry key exists — backfill the Size shown in Programs & Features.
        Task.Run(InstallerInfo.EnsureEstimatedSize);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
