using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Sabeltann;

public static class XamlLoader
{
    public static void Load(this Control control)
    {
        try
        {
            AvaloniaXamlLoader.Load(control);
            var fields = control.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                var found = control.FindControl<Control>(field.Name);
                if (found is not null && field.FieldType.IsAssignableFrom(found.GetType()))
                    field.SetValue(control, found);
            }
        }
        catch
        {
            // XAML resource not available — running with no UI (InitializeComponent fallback)
        }
    }

    public static void Load(this Application app)
    {
        try
        {
            AvaloniaXamlLoader.Load(app);
        }
        catch
        {
            // XAML resource not available — running with no UI (InitializeComponent fallback)
        }
    }
}
