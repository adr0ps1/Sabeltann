using Avalonia.Controls;

namespace Sabeltann;

public partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
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
