using System;
using System.IO;
using System.Text.Json;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed class LocalCopy
{
    private readonly string _path;

    public LocalCopy(string path)
    {
        _path = path;
    }

    public LocalSave? Read(string playerId)
    {
        try
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            LocalSave? local = JsonSerializer.Deserialize<LocalSave>(File.ReadAllText(_path), SaveCodec.Options);
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
        string temporary = _path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(local, SaveCodec.Options));
            File.Move(temporary, _path, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Copie locale non enregistrée : {exception.Message}");
        }
    }

    public void Forget()
    {
        try
        {
            File.Delete(_path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Copie locale non effacée : {exception.Message}");
        }
    }
}
