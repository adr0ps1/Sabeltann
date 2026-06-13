using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Sabeltann.Views;

public partial class ConnectionPage : UserControl
{
    public event EventHandler<RoutedEventArgs>? LoadM3UFileRequested;
    public event EventHandler<RoutedEventArgs>? LoadM3UUrlRequested;
    public event EventHandler<RoutedEventArgs>? XtreamLoginRequested;

    public ConnectionPage()
    {
        InitializeComponent();
        M3UFileBtn.Click += (s, e) => LoadM3UFileRequested?.Invoke(s, e);
        M3UUrlBtn.Click += (s, e) => LoadM3UUrlRequested?.Invoke(s, e);
        XtreamBtn.Click += (s, e) => XtreamLoginRequested?.Invoke(s, e);
    }
}
