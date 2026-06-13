using Sabeltann;

using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Sabeltann;

public partial class InputDialog : Window
{
    public InputDialog()
    {
        this.Load();
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






