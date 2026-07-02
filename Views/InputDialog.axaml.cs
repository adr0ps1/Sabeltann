
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;


namespace Sabeltann;

public partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
    }

    private void OnChromeDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    public InputDialog(string prompt, string defaultValue) : this()
    {
        PromptText.Text = prompt;
        InputBox.Text = defaultValue;
    }

    private void OnOk(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(InputBox.Text);
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }
}










