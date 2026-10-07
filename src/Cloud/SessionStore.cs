using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;

namespace IdleBar.Cloud;

public sealed class SessionStore
{
    private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("IdleBar.Session");

    private readonly string _path;

    public SessionStore(string path)
    {
        _path = path;
    }

    public AuthSession? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AuthSession>(Unprotect(File.ReadAllBytes(_path)));
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            GD.PushWarning($"Session de synchronisation illisible, reconnexion nécessaire : {exception.Message}");
            File.Delete(_path);
            return null;
        }
    }

    public void Save(AuthSession session)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(session);
        string temporaryPath = $"{_path}.tmp";
        WritePrivately(temporaryPath, Protect(json));
        File.Move(temporaryPath, _path, overwrite: true);
    }

    public void Clear()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private static void WritePrivately(string path, byte[] content)
    {
        File.Delete(path);
        FileStreamOptions options = new() { Mode = FileMode.CreateNew, Access = System.IO.FileAccess.Write };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = OwnerOnly;
        }

        using FileStream stream = new(path, options);
        stream.Write(content);
    }

    private static byte[] Protect(byte[] json) =>
        OperatingSystem.IsWindows() ? ProtectedData.Protect(json, Entropy, DataProtectionScope.CurrentUser) : json;

    private static byte[] Unprotect(byte[] stored) =>
        OperatingSystem.IsWindows() ? ProtectedData.Unprotect(stored, Entropy, DataProtectionScope.CurrentUser) : stored;
}
