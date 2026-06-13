using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Sabeltann.Views;

public partial class ContentPicker : UserControl
{
    public event EventHandler<RoutedEventArgs>? LiveTvSelected;
    public event EventHandler<RoutedEventArgs>? VodSelected;
    public event EventHandler<string>? SearchRequested;

    public ContentPicker()
    {
        InitializeComponent();
        LiveTvBtn.Click += (s, e) => LiveTvSelected?.Invoke(s, e);
        VodBtn.Click += (s, e) => VodSelected?.Invoke(s, e);
        SearchBox.KeyDown += OnSearchKeyDown;
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SearchRequested?.Invoke(this, SearchBox.Text ?? "");
            e.Handled = true;
        }
    }
}
