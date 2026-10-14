using System;

namespace IdleBar.Cloud;

public sealed record ReleaseAsset(string Name, string BrowserDownloadUrl, long Size, string? Digest)
{
    private const string Sha256Prefix = "sha256:";

    public string? Sha256 =>
        Digest is not null && Digest.StartsWith(Sha256Prefix, StringComparison.Ordinal) ? Digest[Sha256Prefix.Length..] : null;
}
