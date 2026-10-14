using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;

namespace IdleBar.Desktop;

public sealed class SelfInstaller
{
    private const string ExecutableName = "IdleBar.exe";
    private const string PackName = "IdleBar.pck";
    private const string WorkName = ".idlebar-update";
    private const string ArchiveName = "package.zip";
    private const string StagingName = "new";
    private const string RetiredName = "old";

    private readonly string _install;
    private readonly string _work;

    private SelfInstaller(string install)
    {
        _install = install;
        _work = Path.Combine(install, WorkName);
    }

    private string Staging => Path.Combine(_work, StagingName);

    public static SelfInstaller? ForThisBuild()
    {
        if (!OperatingSystem.IsWindows() || !OS.HasFeature("template"))
        {
            return null;
        }

        string executable = OS.GetExecutablePath();
        string? install = Path.GetDirectoryName(executable);
        bool packaged = install is not null
            && string.Equals(Path.GetFileName(executable), ExecutableName, StringComparison.OrdinalIgnoreCase)
            && File.Exists(Path.Combine(install, PackName));
        return packaged ? new SelfInstaller(install!) : null;
    }

    public async Task PrepareAsync(ReleaseAsset package, IProgress<double> progress)
    {
        if (Directory.Exists(_work))
        {
            await Task.Run(() => Directory.Delete(_work, recursive: true));
        }

        string archive = Path.Combine(_work, ArchiveName);
        await PackageDownload.SaveAsync(package, archive, progress);
        await Task.Run(() => ZipFile.ExtractToDirectory(archive, Staging));
        File.Delete(archive);
        if (!File.Exists(Path.Combine(Staging, ExecutableName)) || !File.Exists(Path.Combine(Staging, PackName)))
        {
            throw new UpdateFailedException("Le paquet téléchargé ne contient pas le jeu.");
        }
    }

    public Task InstallAsync() => Task.Run(() => PackageSwap.Apply(Staging, _install, Path.Combine(_work, RetiredName)));

    public void Relaunch()
    {
        if (OS.CreateProcess(Path.Combine(_install, ExecutableName), []) == -1)
        {
            GD.PushError("Redémarrage d'IdleBar impossible : relance-le à la main.");
        }
    }

    public Task CleanUpAsync() => Directory.Exists(_work) ? Task.Run(DeleteLeftovers) : Task.CompletedTask;

    private void DeleteLeftovers()
    {
        try
        {
            Directory.Delete(_work, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.Print($"Anciens fichiers de mise à jour encore occupés : {exception.Message}");
        }
    }
}
