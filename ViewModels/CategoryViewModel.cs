using CommunityToolkit.Mvvm.ComponentModel;

namespace Sabeltann.ViewModels;

public partial class CategoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private bool _isSelected;

    public List<ChannelListItemViewModel> Channels { get; } = [];
}
