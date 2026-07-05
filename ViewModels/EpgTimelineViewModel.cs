using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sabeltann.Models;
using Sabeltann.Services;

namespace Sabeltann.ViewModels;

/// <summary>One programme block on the timeline, pre-positioned in pixels for the Canvas.</summary>
public class EpgProgrammeViewModel
{
    public required string Title { get; init; }
    public required string TimeRange { get; init; }
    public string? Description { get; init; }
    public double Left { get; init; }
    public double Width { get; init; }
    public bool IsNow { get; init; }
    public required ChannelListItemViewModel Channel { get; init; }
}

/// <summary>One channel row: its name/logo plus the programme blocks that fall in the window.</summary>
public class EpgRowViewModel
{
    public required ChannelListItemViewModel Channel { get; init; }
    public string Name => Channel.Name;
    public ObservableCollection<EpgProgrammeViewModel> Programmes { get; } = [];
}

/// <summary>Ruler tick — an hour label positioned along the timeline.</summary>
public record EpgHourMarker(double Left, string Label);

public partial class EpgTimelineViewModel : ObservableObject
{
    // 5 px/min → one hour is 300 px wide.
    public const double PxPerMinute = 5.0;
    private const int WindowHours = 24;
    private const int MaxRows = 300;

    private readonly EpgService _epg = new();

    /// <summary>Positions the now-line at its pixel offset inside the timeline canvas.</summary>
    public static readonly FuncValueConverter<double, Thickness> LeftMargin = new(px => new Thickness(px, 0, 0, 0));

    public event Action<ChannelListItemViewModel>? PlayChannelRequested;

    public ObservableCollection<EpgRowViewModel> Rows { get; } = [];
    public ObservableCollection<EpgHourMarker> HourMarkers { get; } = [];

    [ObservableProperty] private double _totalWidth;
    [ObservableProperty] private double _nowLineLeft;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private string _status = "";

    private DateTime _origin;

    [RelayCommand]
    private void PlayProgramme(EpgProgrammeViewModel? programme)
    {
        if (programme is not null)
            PlayChannelRequested?.Invoke(programme.Channel);
    }

    /// <summary>Fetch the XMLTV guide and build channel rows joined by tvg-id. Safe to re-call.</summary>
    public async Task LoadAsync(XtreamConnectionInfo info, IReadOnlyList<ChannelListItemViewModel> channels)
    {
        IsLoading = true;
        Status = "Loading guide…";
        Rows.Clear();
        HourMarkers.Clear();

        var guide = await _epg.FetchAsync(info.XmltvUrl);
        Build(guide, channels);

        IsLoading = false;
        HasData = Rows.Count > 0;
        Status = HasData
            ? $"{Rows.Count} channels with guide data"
            : "No EPG data available for this source.";
    }

    private void Build(Dictionary<string, List<Programme>> guide, IReadOnlyList<ChannelListItemViewModel> channels)
    {
        var now = DateTime.UtcNow;
        // Start one hour before now, floored to the half hour, so "now" sits just inside the view.
        _origin = FloorToHalfHour(now.AddHours(-1));
        var end = _origin.AddHours(WindowHours);
        TotalWidth = WindowHours * 60 * PxPerMinute;
        NowLineLeft = (now - _origin).TotalMinutes * PxPerMinute;

        for (var t = _origin; t <= end; t = t.AddHours(1))
            HourMarkers.Add(new EpgHourMarker((t - _origin).TotalMinutes * PxPerMinute, t.ToLocalTime().ToString("HH:mm")));

        foreach (var ch in channels)
        {
            if (ch.TvgId is null || !guide.TryGetValue(ch.TvgId, out var programmes)) continue;

            var row = new EpgRowViewModel { Channel = ch };
            foreach (var p in programmes)
            {
                if (p.StopUtc <= _origin || p.StartUtc >= end) continue; // outside the window

                var start = p.StartUtc < _origin ? _origin : p.StartUtc;
                var stop = p.StopUtc > end ? end : p.StopUtc;
                row.Programmes.Add(new EpgProgrammeViewModel
                {
                    Title = p.Title,
                    Description = p.Description,
                    TimeRange = $"{p.StartUtc.ToLocalTime():HH:mm}–{p.StopUtc.ToLocalTime():HH:mm}",
                    Left = (start - _origin).TotalMinutes * PxPerMinute,
                    Width = Math.Max(1, (stop - start).TotalMinutes * PxPerMinute),
                    IsNow = p.IsAiringAt(now),
                    Channel = ch,
                });
            }

            if (row.Programmes.Count > 0)
            {
                Rows.Add(row);
                if (Rows.Count >= MaxRows) break;
            }
        }
    }

    private static DateTime FloorToHalfHour(DateTime t)
        => new(t.Year, t.Month, t.Day, t.Hour, t.Minute < 30 ? 0 : 30, 0, DateTimeKind.Utc);
}
