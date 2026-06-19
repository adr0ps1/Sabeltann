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
    private bool _imageLoaded;
    private bool _imageLoading;

    public ChannelListItemViewModel(Channel channel)
    {
        Name = channel.Name;
        Logo = channel.Logo;
        Group = channel.Group;
        Url = channel.Url;
        Type = channel.Type;
    }

    public void EnsureImageLoaded()
    {
        if (_imageLoaded || _imageLoading || string.IsNullOrWhiteSpace(Logo)) return;
        _imageLoading = true;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var bmp = await ImageService.LoadAsync(Logo);
        _imageLoading = false;
        if (bmp is not null)
        {
            _imageLoaded = true;
            LogoSrc = bmp;
        }
    }
}
