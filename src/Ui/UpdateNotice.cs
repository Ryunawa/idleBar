using System;
using Godot;
using IdleBar.Cloud;
using HttpClient = System.Net.Http.HttpClient;

namespace IdleBar.Ui;

public partial class UpdateNotice : Node
{
    private const double FirstCheckSeconds = 5;
    private const double CheckIntervalSeconds = 6 * 3600;
    private const int TimeoutSeconds = 10;

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
    private ReleaseFeed? _feed;
    private Version _current = new(0, 0, 0);
    private double _untilCheck = FirstCheckSeconds;
    private bool _checking;

    public event Action<ReleaseInfo>? Found;

    public ReleaseInfo? Latest { get; private set; }

    public override void _Ready()
    {
        _feed = ReleaseFeed.FromProjectSettings(_http);
        _current = GameVersion.Current;
    }

    public override void _Process(double delta)
    {
        if (_feed is null || _checking)
        {
            return;
        }

        _untilCheck -= delta;
        if (_untilCheck <= 0)
        {
            _untilCheck = CheckIntervalSeconds;
            Check(_feed);
        }
    }

    public override void _ExitTree() => _http.Dispose();

    private async void Check(ReleaseFeed feed)
    {
        _checking = true;
        try
        {
            ReleaseInfo? release = await feed.FetchLatestAsync();
            if (release is { Version: Version version, HasSafePage: true } && version > _current && release != Latest)
            {
                Latest = release;
                Found?.Invoke(release);
            }
        }
        catch (Exception exception) when (TransportFailure.Matches(exception))
        {
            GD.Print($"Recherche de mise à jour impossible : {exception.Message}");
        }
        finally
        {
            _checking = false;
        }
    }
}