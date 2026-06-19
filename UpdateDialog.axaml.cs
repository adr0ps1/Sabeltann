using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Sabeltann;

public partial class UpdateDialog : Window
{
    public bool RestartRequested { get; private set; }

    public UpdateDialog()
    {
        InitializeComponent();
    }

    public string Version { get; set; } = "";

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        MessageText.Text = $"Sabeltann {Version} has been downloaded. Restart now to apply the update?";
    }

    private void OnRestart(object? sender, RoutedEventArgs e)
    {
        RestartRequested = true;
        Close();
    }

    private void OnLater(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}