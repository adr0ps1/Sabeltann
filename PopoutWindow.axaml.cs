using Avalonia.Controls;

namespace Sabeltann;

/// <summary>
/// A detached, always-on-top video window that shares MainViewModel's <c>VideoBitmap</c>. It renders
/// nothing on its own — MainWindow calls <see cref="InvalidateVideo"/> on each decoded frame so this
/// window repaints in lockstep with the inline surface.
/// </summary>
public partial class PopoutWindow : Window
{
    public PopoutWindow()
    {
        InitializeComponent();
    }

    public void InvalidateVideo() => PopoutImage?.InvalidateVisual();
}
