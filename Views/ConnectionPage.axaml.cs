using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Sabeltann.Views;

public partial class ConnectionPage : UserControl
{
    public event EventHandler<RoutedEventArgs>? LoadM3UFileRequested;
    public event EventHandler<RoutedEventArgs>? LoadM3UUrlRequested;
    public event EventHandler<RoutedEventArgs>? XtreamLoginRequested;

    public ConnectionPage()
    {
        AvaloniaXamlLoader.Load(this);

        var m3uFileBtn = this.FindControl<Button>("M3UFileBtn");
        var m3uUrlBtn = this.FindControl<Button>("M3UUrlBtn");
        var xtreamBtn = this.FindControl<Button>("XtreamBtn");

        if (m3uFileBtn is not null)
            m3uFileBtn.Click += (s, e) => LoadM3UFileRequested?.Invoke(s, e);
        if (m3uUrlBtn is not null)
            m3uUrlBtn.Click += (s, e) => LoadM3UUrlRequested?.Invoke(s, e);
        if (xtreamBtn is not null)
            xtreamBtn.Click += (s, e) => XtreamLoginRequested?.Invoke(s, e);
    }
}










