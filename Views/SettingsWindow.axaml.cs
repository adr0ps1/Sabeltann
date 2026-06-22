using Avalonia.Interactivity;
using Avalonia.Controls;

using Sabeltann.Services;

namespace Sabeltann.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsData _data;

    public SettingsWindow(SettingsData current, string? serverUrl, string? username, int channelCount)
    {
        InitializeComponent();
        _data = current;
        VolumeSlider.Value = current.DefaultVolume;
        AutoLoadCheck.IsChecked = current.AutoLoadLastSession;
        CheckForUpdatesCheck.IsChecked = current.CheckForUpdatesEnabled;
        IncludePrereleaseCheck.IsChecked = current.IncludePrerelease;
        ServerUrlText.Text = serverUrl ?? "Not connected";
        UsernameText.Text = username ?? "-";
        ChannelCountText.Text = $"{channelCount} channels";
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        _data.DefaultVolume = (int)VolumeSlider.Value;
        _data.AutoLoadLastSession = AutoLoadCheck.IsChecked ?? true;
        _data.CheckForUpdatesEnabled = CheckForUpdatesCheck.IsChecked ?? true;
        _data.IncludePrerelease = IncludePrereleaseCheck.IsChecked ?? false;
        Close(_data);
    }
}










