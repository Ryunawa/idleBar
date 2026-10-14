using System;
using System.IO;
using System.Text.Json;
using IdleBar.Cloud;
using IdleBar.Desktop;
using Xunit;

namespace IdleBar.Tests;

public sealed class UpdateTests : IDisposable
{
    private const string Digest = "cb49f96e6d8c55caadf32ba54fb0c25efee5a460c5610dffb04ca2356b03f9bb";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"idlebar-update-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void TheLatestReleaseGivesTheWindowsPackage()
    {
        ReleaseInfo release = Release("https://github.com/Ryunawa/idleBar/releases/download/v0.8.2/IdleBar-v0.8.2-windows.zip", $"sha256:{Digest}");

        ReleaseAsset? package = release.WindowsPackage;

        Assert.NotNull(package);
        Assert.Equal(Digest, package.Sha256);
        Assert.Equal(73430350, package.Size);
    }

    [Fact]
    public void APackageFromElsewhereOrWithoutDigestIsRefused()
    {
        Assert.Null(Release("https://github.com/someone/else/releases/download/v0.8.2/IdleBar-v0.8.2-windows.zip", $"sha256:{Digest}").WindowsPackage);
        Assert.Null(Release("https://github.com/Ryunawa/idleBar/releases/download/v0.8.2/IdleBar-v0.8.2-windows.zip", null).WindowsPackage);
    }

    [Fact]
    public void TheSwapPutsTheNewFilesInPlaceAndTheOldOnesAside()
    {
        string install = Folder("install", ("IdleBar.exe", "old exe"), ("data/game.dll", "old dll"), ("notes.txt", "mine"));
        string staging = Folder("staging", ("IdleBar.exe", "new exe"), ("data/game.dll", "new dll"), ("data/added.dll", "added"));
        string retired = Path.Combine(_root, "retired");

        PackageSwap.Apply(staging, install, retired);

        Assert.Equal("new exe", File.ReadAllText(Path.Combine(install, "IdleBar.exe")));
        Assert.Equal("new dll", File.ReadAllText(Path.Combine(install, "data", "game.dll")));
        Assert.Equal("added", File.ReadAllText(Path.Combine(install, "data", "added.dll")));
        Assert.Equal("mine", File.ReadAllText(Path.Combine(install, "notes.txt")));
        Assert.Equal("old exe", File.ReadAllText(Path.Combine(retired, "IdleBar.exe")));
        Assert.Equal("old dll", File.ReadAllText(Path.Combine(retired, "data", "game.dll")));
    }

    [Fact]
    public void AFailedSwapPutsTheOldFilesBack()
    {
        string install = Folder("install", ("IdleBar.exe", "old exe"));
        Directory.CreateDirectory(Path.Combine(install, "IdleBar.pck"));
        string staging = Folder("staging", ("IdleBar.exe", "new exe"), ("IdleBar.pck", "new pck"));

        Exception? failure = Record.Exception(() => PackageSwap.Apply(staging, install, Path.Combine(_root, "retired")));

        Assert.True(failure is IOException or UnauthorizedAccessException);
        Assert.Equal("old exe", File.ReadAllText(Path.Combine(install, "IdleBar.exe")));
    }

    private static ReleaseInfo Release(string downloadUrl, string? digest)
    {
        string json = JsonSerializer.Serialize(new
        {
            tag_name = "v0.8.2",
            html_url = "https://github.com/Ryunawa/idleBar/releases/tag/v0.8.2",
            assets = new[]
            {
                new { name = "IdleBar-v0.8.2-macos.zip", browser_download_url = "https://github.com/Ryunawa/idleBar/releases/download/v0.8.2/IdleBar-v0.8.2-macos.zip", size = 127195342L, digest = (string?)"sha256:eec7" },
                new { name = "IdleBar-v0.8.2-windows.zip", browser_download_url = downloadUrl, size = 73430350L, digest },
            },
        });
        return JsonSerializer.Deserialize<ReleaseInfo>(json, SupabaseJson.Options)!;
    }

    private string Folder(string name, params (string Relative, string Content)[] files)
    {
        string folder = Path.Combine(_root, name);
        foreach ((string relative, string content) in files)
        {
            string path = Path.Combine(folder, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        Directory.CreateDirectory(folder);
        return folder;
    }
}
