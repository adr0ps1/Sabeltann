using Avalonia.Controls;
using Avalonia.Input;

namespace Sabeltann;

/// <summary>
/// A detached, always-on-top video window that shares MainViewModel's <c>VideoBitmap</c>. It renders
/// nothing on its own — MainWindow calls <see cref="InvalidateVideo"/> on each decoded frame so this
/// window repaints in lockstep with the inline surface. Borderless (no OS title bar); the top strip
/// drags the window and its ✕ closes it (which returns the video inline).
/// </summary>
public partial class PopoutWindow : Window
{
    public PopoutWindow()
    {
        InitializeComponent();
    }

    public void InvalidateVideo() => PopoutImage?.InvalidateVisual();

    private void OnDragStrip(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnClosePopout(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void OnResizePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.Tag is string edgeName &&
            System.Enum.TryParse<WindowEdge>(edgeName, out var edge) &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(edge, e);
    }
}
