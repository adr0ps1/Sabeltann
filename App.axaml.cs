using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Sabeltann.Services;

namespace Sabeltann;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        InstallGlobalExceptionHandlers();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void InstallGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                LogService.Error("AppDomain unhandled exception", new
                {
                    type = ex?.GetType().Name,
                    message = ex?.Message,
                    stack = ex?.StackTrace,
                    isTerminating = e.IsTerminating
                });
            }
            catch { }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            try
            {
                LogService.Error("Unobserved task exception", new
                {
                    type = e.Exception.GetType().Name,
                    message = e.Exception.Message,
                    stack = e.Exception.StackTrace
                });
            }
            catch { }
            e.SetObserved();
        };

        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            try
            {
                LogService.Error("UI thread unhandled exception", new
                {
                    type = e.Exception.GetType().Name,
                    message = e.Exception.Message,
                    stack = e.Exception.StackTrace
                });
            }
            catch { }
            e.Handled = true;
        };
    }
}
