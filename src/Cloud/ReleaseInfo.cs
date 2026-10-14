using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Cloud;

public sealed record ReleaseInfo(string TagName, string HtmlUrl, IReadOnlyList<ReleaseAsset>? Assets)
{
    private const string DownloadSite = "https://github.com/";
    private const string PagePath = "/releases/tag/";
    private const string DownloadPath = "/releases/download/";

    public Version? Version => Version.TryParse(TagName.TrimStart('v', 'V'), out Version? version) ? version : null;

    public string Name => TagName.TrimStart('v', 'V');

    public bool HasSafePage => HtmlUrl.StartsWith(DownloadSite, StringComparison.Ordinal);

    public ReleaseAsset? WindowsPackage
    {
        get
        {
            if (!HasSafePage || Assets is null)
            {
                return null;
            }

            string name = $"IdleBar-{TagName}-windows.zip";
            string url = $"{HtmlUrl.Replace(PagePath, DownloadPath, StringComparison.Ordinal)}/{name}";
            return Assets.FirstOrDefault(asset =>
                asset.Name == name && asset.BrowserDownloadUrl == url && asset.Size > 0 && asset.Sha256 is not null);
        }
    }
}
