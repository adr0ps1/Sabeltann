using CommunityToolkit.Mvvm.ComponentModel;
using Sabeltann.Models;

namespace Sabeltann.ViewModels;

public partial class ChannelListItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _logo;

    [ObservableProperty]
    private string? _group;

    [ObservableProperty]
    private bool _isFavorite;

    public string Url { get; }

    public ChannelListItemViewModel(Channel channel)
    {
        Name = channel.Name;
        Logo = channel.Logo;
        Group = channel.Group;
        Url = channel.Url;
    }
}
