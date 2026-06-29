using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Sabeltann.ViewModels;

namespace Sabeltann.Views;

public partial class VodBrowser : UserControl
{
    public VodBrowser()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fetches the OMDb poster the first time a card scrolls into view, so we only spend an
    /// API request per title the user actually looks at (1k/day OMDb free-tier budget).
    /// </summary>
    private void OnPosterViewport(object? sender, EffectiveViewportChangedEventArgs e)
    {
        if (sender is not Control c || c.DataContext is not VodMovieViewModel vm) return;
        var vp = e.EffectiveViewport;
        if (vp.Width <= 0 || vp.Height <= 0) return;
        if (!vp.Intersects(new Rect(c.Bounds.Size))) return;
        vm.RequestOmdbPoster();
    }
}
