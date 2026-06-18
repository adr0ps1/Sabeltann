using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;
using Sabeltann.Models;
using Sabeltann.Services;

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

    [ObservableProperty]
    private Bitmap? _logoSrc;

    public string Url { get; }
    public ChannelType Type { get; }

    public ChannelListItemViewModel(Channel channel)
    {
        Name = channel.Name;
        Logo = channel.Logo;
        Group = channel.Group;
        Url = channel.Url;
        Type = channel.Type;
    }

    public void BeginLoadImage()
    {
        if (!string.IsNullOrWhiteSpace(Logo))
            _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var bmp = await ImageService.LoadAsync(Logo);
        if (bmp is not null)
            LogoSrc = bmp;
    }
}
