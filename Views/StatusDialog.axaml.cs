using System;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Sabeltann.Views;

public partial class StatusDialog : Window
{
    private string _plainText = "";

    // Parameterless ctor for the XAML runtime loader.
    public StatusDialog() { InitializeComponent(); }

    public StatusDialog(string version, int channelCount) : this()
    {
        var installPath = AppContext.BaseDirectory;
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sabeltann", "settings.json");
        var logsPath = Path.Combine(installPath, "logs");

        VersionText.Text = version;
        InstalledText.Text = SafeInstalledOn(installPath);
        SizeText.Text = SafeSize(installPath);
        PathText.Text = installPath;
        SettingsText.Text = settingsPath;
        LogsText.Text = logsPath;
        RuntimeText.Text = $".NET {Environment.Version}";
        ChannelsText.Text = $"{channelCount}";

        _plainText =
            $"Version: {version}\nInstalled: {InstalledText.Text}\nInstall size: {SizeText.Text}\n" +
            $"Install path: {installPath}\nSettings: {settingsPath}\nLogs: {logsPath}\n" +
            $"Runtime: {RuntimeText.Text}\nChannels: {channelCount}";
    }

    private static string SafeInstalledOn(string dir)
    {
        try { return Directory.GetCreationTime(dir).ToString("yyyy-MM-dd HH:mm"); }
        catch { return "—"; }
    }

    private static string SafeSize(string dir)
    {
        try
        {
            long bytes = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                .Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
            return $"{bytes / 1024.0 / 1024.0:0.#} MB";
        }
        catch { return "—"; }
    }

    private void OnChromeDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private async void OnCopy(object? sender, RoutedEventArgs e)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
            await clipboard.SetTextAsync(_plainText);
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
