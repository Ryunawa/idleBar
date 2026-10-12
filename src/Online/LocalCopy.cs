using System;
using System.IO;
using System.Text.Json;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed class LocalCopy
{
    private readonly string _folder;

    public LocalCopy(string folder)
    {
        _folder = folder;
    }

    public LocalSave? Read(string playerId)
    {
        string path = PathOf(playerId);
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            LocalSave? local = JsonSerializer.Deserialize<LocalSave>(File.ReadAllText(path), SaveCodec.Options);
            return local is not null && local.PlayerId == playerId && SaveCodec.Complete(local.Save) ? local : null;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Copie locale illisible : {exception.Message}");
            return null;
        }
    }

    public void Write(LocalSave local)
    {
        string path = PathOf(local.PlayerId);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(local, SaveCodec.Options));
            File.Move(temporary, path, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Copie locale non enregistrée : {exception.Message}");
        }
    }

    public void Forget(string playerId)
    {
        try
        {
            File.Delete(PathOf(playerId));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Copie locale non effacée : {exception.Message}");
        }
    }

    private string PathOf(string playerId)
    {
        string safe = string.Concat(playerId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_folder, $"frontier-{safe}.json");
    }
}
