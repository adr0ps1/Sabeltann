using Avalonia.Controls;
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
        SearchBox.TextChanged += (_, _) => SearchRequested?.Invoke(this, SearchBox.Text ?? "");
    }
}
