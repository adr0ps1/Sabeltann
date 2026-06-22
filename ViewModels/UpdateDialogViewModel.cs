using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sabeltann.ViewModels;

public partial class UpdateDialogViewModel : ObservableObject
{
    [ObservableProperty] private string _currentVersion = "";
    [ObservableProperty] private string _newVersion = "";
    [ObservableProperty] private string? _releaseNotes;
    [ObservableProperty] private bool _hasReleaseNotes;

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
        Result = UpdateDialogResult.InstallAndRestart;
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void RemindLater()
    {
        Result = UpdateDialogResult.RemindLater;
        CloseRequested?.Invoke();
    }
}

public enum UpdateDialogResult { InstallOnExit, InstallAndRestart, RemindLater }
