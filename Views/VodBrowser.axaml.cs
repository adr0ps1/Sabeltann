using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Sabeltann.ViewModels;

namespace Sabeltann.Views;

public partial class VodBrowser : UserControl
{
    public VodBrowser()
    {
        InitializeComponent();
    }

    // Remove a card from the Continue Watching strip. A menu popup doesn't reliably inherit
    // the item DataContext, so resolve the movie from the item's DataContext or, failing that,
    // the owning ContextMenu's PlacementTarget.
    private void OnRemoveContinueWatching(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        var movie = mi.DataContext as VodMovieViewModel
                    ?? (mi.FindLogicalAncestorOfType<ContextMenu>()?.PlacementTarget as Control)?.DataContext as VodMovieViewModel;
        movie?.OnRemove?.Invoke(movie);
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
