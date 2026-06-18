using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Sabeltann.Views;

public partial class ContentPicker : UserControl
{
    public event EventHandler<RoutedEventArgs>? LiveTvSelected;
    public event EventHandler<RoutedEventArgs>? MoviesSelected;
    public event EventHandler<RoutedEventArgs>? SeriesSelected;

    public ContentPicker()
    {
        InitializeComponent();
        LiveTvBtn.Click += (s, e) => LiveTvSelected?.Invoke(s, e);
        MoviesBtn.Click += (s, e) => MoviesSelected?.Invoke(s, e);
        SeriesBtn.Click += (s, e) => SeriesSelected?.Invoke(s, e);
    }
}
