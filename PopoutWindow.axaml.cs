using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Sabeltann;

/// <summary>
/// A detached, always-on-top video window that shares MainViewModel's <c>VideoBitmap</c>. It renders
/// nothing on its own — MainWindow calls <see cref="InvalidateVideo"/> on each decoded frame so this
/// window repaints in lockstep with the inline surface. Borderless (no OS title bar); the top of the
/// window drags it, and the ✕ / media controls fade in on mouse movement and auto-hide.
/// </summary>
public partial class PopoutWindow : Window
{
    private readonly DispatcherTimer _hideChrome;

    public PopoutWindow()
    {
        InitializeComponent();
        _hideChrome = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _hideChrome.Tick += (_, _) => { Chrome.IsVisible = false; _hideChrome.Stop(); };
        PointerMoved += (_, _) => ShowChrome();
        Opened += (_, _) => ShowChrome();
    }

    public void InvalidateVideo() => PopoutImage?.InvalidateVisual();

    // Reveal the ✕ + controls and restart the idle countdown.
    private void ShowChrome()
    {
        Chrome.IsVisible = true;
        _hideChrome.Stop();
        _hideChrome.Start();
    }

    private void OnDragStrip(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnClosePopout(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void OnResizePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.Tag is string edgeName &&
            Enum.TryParse<WindowEdge>(edgeName, out var edge) &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(edge, e);
    }
}
