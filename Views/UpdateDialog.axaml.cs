using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Sabeltann.Views;

public partial class UpdateDialog : Window
{
    public UpdateDialog()
    {
        InitializeComponent();
    }

    private void OnChromeDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnChromeClose(object? sender, RoutedEventArgs e) => Close();
}
