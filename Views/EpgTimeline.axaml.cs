using Avalonia;
using Avalonia.Controls;

namespace Sabeltann.Views;

public partial class EpgTimeline : UserControl
{
    public EpgTimeline()
    {
        InitializeComponent();
        // Keep the frozen name column and the ruler aligned with the scrolling timeline body.
        BodyScroll.ScrollChanged += (_, _) =>
        {
            NameScroll.Offset = new Vector(0, BodyScroll.Offset.Y);
            RulerScroll.Offset = new Vector(BodyScroll.Offset.X, 0);
        };
    }
}
