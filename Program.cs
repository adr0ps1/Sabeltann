using Avalonia;
using Sentry;

namespace Sabeltann;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using (SentrySdk.Init(o =>
        {
            o.Dsn = "https://9dcb3fbc68fd57d49251e7d2e483de2a@o4511547975335936.ingest.de.sentry.io/4511547984314448";
            o.TracesSampleRate = 1.0;
            o.Debug = false;
        }))
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
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
