using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Sabeltann;

public static class XamlLoader
{
    public static void Load(this Control control)
    {
        AvaloniaXamlLoader.Load(control);
    }

    public static void Load(this Application app)
    {
        AvaloniaXamlLoader.Load(app);
    }
}
