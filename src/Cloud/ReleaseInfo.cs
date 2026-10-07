using System;

namespace IdleBar.Cloud;

public sealed record ReleaseInfo(string TagName, string HtmlUrl)
{
    private const string DownloadSite = "https://github.com/";

    public Version? Version => Version.TryParse(TagName.TrimStart('v', 'V'), out Version? version) ? version : null;

    public string Name => TagName.TrimStart('v', 'V');

    public bool HasSafePage => HtmlUrl.StartsWith(DownloadSite, StringComparison.Ordinal);
}