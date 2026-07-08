using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sabeltann.ViewModels;

public partial class UpdateDialogViewModel : ObservableObject
{
    [ObservableProperty] private string _currentVersion = "";
    [ObservableProperty] private string _newVersion = "";
    [ObservableProperty] private string? _releaseNotes;
    [ObservableProperty] private bool _hasReleaseNotes;

    // True once "Install & Restart" is pressed: greys the buttons and shows the installing status
    // so the (silent, post-exit) update doesn't look like nothing happened. (#80)
    [ObservableProperty] private bool _isInstalling;

    public UpdateDialogResult Result { get; private set; } = UpdateDialogResult.RemindLater;

    public event Action? CloseRequested;

    [RelayCommand]
    private void InstallOnExit()
    {
        Result = UpdateDialogResult.InstallOnExit;
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void InstallAndRestart()
    {
        IsInstalling = true;
        Result = UpdateDialogResult.InstallAndRestart;
        // Defer one tick so the greyed/installing state paints before the host applies + shuts down.
        Dispatcher.UIThread.Post(() => CloseRequested?.Invoke(), DispatcherPriority.Background);
    }

    [RelayCommand]
    private void RemindLater()
    {
        Result = UpdateDialogResult.RemindLater;
        CloseRequested?.Invoke();
    }
}

public enum UpdateDialogResult { InstallOnExit, InstallAndRestart, RemindLater }
