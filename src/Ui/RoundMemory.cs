using System;
using Godot;

namespace IdleBar.Ui;

public sealed class RoundMemory
{
    private const string Path = "user://rounds.cfg";
    private const string Section = "tournees";
    private const string Key = "derniere";

    private DateTimeOffset _last;

    public RoundMemory()
    {
        ConfigFile file = new();
        file.Load(Path);
        _last = DateTimeOffset.FromUnixTimeSeconds(file.GetValue(Section, Key, 0L).AsInt64());
    }

    public bool IsNew(DateTimeOffset at) => at.ToUnixTimeSeconds() > _last.ToUnixTimeSeconds();

    public void Remember(DateTimeOffset at)
    {
        if (!IsNew(at))
        {
            return;
        }

        _last = at;
        ConfigFile file = new();
        file.SetValue(Section, Key, at.ToUnixTimeSeconds());
        file.Save(Path);
    }
}
