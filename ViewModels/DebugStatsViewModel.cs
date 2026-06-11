using System.ComponentModel;
using System.Runtime.CompilerServices;
using Sabeltann.Services;

namespace Sabeltann.ViewModels;

public class DebugStatsViewModel : INotifyPropertyChanged
{
    private PlaybackService? _player;
    private readonly Timer _timer;

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _connectionUrl = "-";
    public string ConnectionUrl { get => _connectionUrl; set => SetField(ref _connectionUrl, value); }

    private string _inputBitrate = "-";
    public string InputBitrate { get => _inputBitrate; set => SetField(ref _inputBitrate, value); }

    private string _demuxBitrate = "-";
    public string DemuxBitrate { get => _demuxBitrate; set => SetField(ref _demuxBitrate, value); }

    private string _sendBitrate = "-";
    public string SendBitrate { get => _sendBitrate; set => SetField(ref _sendBitrate, value); }

    private string _sentPackets = "-";
    public string SentPackets { get => _sentPackets; set => SetField(ref _sentPackets, value); }

    private string _displayedPictures = "-";
    public string DisplayedPictures { get => _displayedPictures; set => SetField(ref _displayedPictures, value); }

    private string _lostPictures = "-";
    public string LostPictures { get => _lostPictures; set => SetField(ref _lostPictures, value); }

    private string _demuxCorrupted = "-";
    public string DemuxCorrupted { get => _demuxCorrupted; set => SetField(ref _demuxCorrupted, value); }

    private string _demuxDiscontinuity = "-";
    public string DemuxDiscontinuity { get => _demuxDiscontinuity; set => SetField(ref _demuxDiscontinuity, value); }

    private string _readBytes = "-";
    public string ReadBytes { get => _readBytes; set => SetField(ref _readBytes, value); }

    private string _state = "Idle";
    public string State { get => _state; set => SetField(ref _state, value); }

    public DebugStatsViewModel(PlaybackService? player)
    {
        _player = player;
        _timer = new Timer(Refresh, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void SetPlayer(PlaybackService? player)
    {
        _player = player;
    }

    private void Refresh(object? _)
    {
        try
        {
            var player = _player?.Player;
            if (player is null) return;

            var media = player.Media;
            State = player.IsPlaying ? "Playing" : player.State.ToString();

            if (media?.Statistics is { } stats)
            {
                InputBitrate = FormatBitrate(stats.InputBitrate);
                DemuxBitrate = FormatBitrate(stats.DemuxBitrate);
                SendBitrate = FormatBitrate(stats.SendBitrate);
                SentPackets = stats.SentPackets.ToString("N0");
                DisplayedPictures = stats.DisplayedPictures.ToString("N0");
                LostPictures = stats.LostPictures.ToString("N0");
                DemuxCorrupted = stats.DemuxCorrupted.ToString("N0");
                DemuxDiscontinuity = stats.DemuxDiscontinuity.ToString("N0");
                ReadBytes = FormatBytes(stats.ReadBytes);
            }
        }
        catch { }
    }

    public void SetUrl(string url)
    {
        ConnectionUrl = url ?? "-";
    }

    public void Stop()
    {
        _timer.Dispose();
    }

    private static string FormatBitrate(float? bits) =>
        bits is null ? "-" : $"{bits / 1000.0:F1} kb/s";

    private static string FormatBytes(int? bytes) =>
        bytes is null ? "-" : bytes >= 1_000_000
            ? $"{bytes / 1_000_000.0:F1} MB"
            : $"{bytes / 1000.0:F1} KB";

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
