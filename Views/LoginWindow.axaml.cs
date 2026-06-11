using Avalonia.Controls;
using Sabeltann.Models;

namespace Sabeltann;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void OnLogin(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var serverUrl = ServerUrlBox.Text?.Trim();
        var username = UsernameBox.Text?.Trim();
        var password = PasswordBox.Text?.Trim();

        if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return;
        }

        Close(new XtreamConnectionInfo
        {
            ServerUrl = serverUrl,
            Username = username,
            Password = password
        });
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }
}
