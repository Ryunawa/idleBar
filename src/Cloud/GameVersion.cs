using System;
using Godot;

namespace IdleBar.Cloud;

public static class GameVersion
{
    private const string VersionSetting = "application/config/version";
    private const string DownloadSetting = "idlebar/updates/download_url";

    public static string Name => ProjectSettings.GetSetting(VersionSetting, string.Empty).AsString();

    public static Version Current => Version.TryParse(Name, out Version? current) ? current : new Version(0, 0, 0);

    public static string DownloadUrl => ProjectSettings.GetSetting(DownloadSetting, string.Empty).AsString();
}
