using Sabeltann;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Sabeltann.Views;

public partial class ContentPicker : UserControl
{
    public event EventHandler<RoutedEventArgs>? LiveTvSelected;
    public event EventHandler<RoutedEventArgs>? VodSelected;

    public ContentPicker()
    {
        this.Load();
        LiveTvBtn.Click += (s, e) => LiveTvSelected?.Invoke(s, e);
        VodBtn.Click += (s, e) => VodSelected?.Invoke(s, e);
    }
}






