using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Sabeltann.ViewModels;

namespace Sabeltann.Views;

public partial class SeriesBrowser : UserControl
{
    public SeriesBrowser()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fetches the OMDb series poster the first time a show card scrolls into view, so we only
    /// spend an API request per series the user actually looks at (1k/day OMDb free-tier budget).
    /// </summary>
    private void OnCoverViewport(object? sender, EffectiveViewportChangedEventArgs e)
    {
        if (sender is not Control c || c.DataContext is not SeriesShowViewModel vm) return;
        var vp = e.EffectiveViewport;
        if (vp.Width <= 0 || vp.Height <= 0) return;
        if (!vp.Intersects(new Rect(c.Bounds.Size))) return;
        vm.RequestOmdbCover();
    }
}
