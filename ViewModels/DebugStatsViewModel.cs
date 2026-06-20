using Avalonia.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Sabeltann.Services;

namespace Sabeltann.ViewModels;

public class DebugStatsViewModel : INotifyPropertyChanged
{
    private PlaybackService? _player;
    private MainViewModel? _mainVm;
    private readonly DispatcherTimer _timer;

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _connectionUrl = "-";
    public string ConnectionUrl { get => _connectionUrl; set => SetField(ref _connectionUrl, value); }

    private string _state = "Idle";
    public string State { get => _state; set => SetField(ref _state, value); }

    private string _resolution = "-";
    public string Resolution { get => _resolution; set => SetField(ref _resolution, value); }

    private string _frames = "-";
    public string Frames { get => _frames; set => SetField(ref _frames, value); }

    private string _position = "-";
    public string Position { get => _position; set => SetField(ref _position, value); }

    private string _duration = "-";
    public string Duration { get => _duration; set => SetField(ref _duration, value); }

    private int _volume;
    public int Volume { get => _volume; set => SetField(ref _volume, value); }

    public DebugStatsViewModel(PlaybackService? player)
    {
        _player = player;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    public void SetPlayer(PlaybackService? player, MainViewModel? mainVm = null)
    {
        _player = player;
        _mainVm = mainVm;
    }

    private void Refresh()
    {
        try
        {
            if (_player is null) return;
            State = _player.IsPlaying ? "Playing" : "Idle";
            Resolution = _player.VideoWidth > 0 ? $"{_player.VideoWidth}x{_player.VideoHeight}" : "-";
            Frames = $"{_player.FramesDecoded}";
            Position = _player.TimeMs > 0 ? FormatTime(_player.TimeMs) : "-";
            Duration = _player.Length > 0 ? FormatTime(_player.Length) : "??:??";
            Volume = _mainVm?.Volume ?? 0;
        }
        catch (Exception ex)
        {
            LogService.Error("DebugStats refresh failed", new { ex.Message, ex.GetType().Name });
        }
    }

    private static string FormatTime(double ms)
    {
        if (ms <= 0) return "--:--";
        var t = TimeSpan.FromMilliseconds(ms);
        return t.TotalHours >= 1 ? $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{t.Minutes:D2}:{t.Seconds:D2}";
    }

    public void SetUrl(string url)
    {
        ConnectionUrl = url ?? "-";
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
