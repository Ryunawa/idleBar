using System.IO;
using System.Text.Json;
using Godot;

namespace IdleBar.Game;

public sealed class SaveStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

    public SaveStore(string path)
    {
        _path = path;
    }

    public SaveData? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SaveData>(File.ReadAllText(_path), JsonOptions);
        }
        catch (JsonException exception)
        {
            string backupPath = $"{_path}.corrupt";
            File.Copy(_path, backupPath, overwrite: true);
            GD.PushWarning($"Sauvegarde illisible, copiée dans {backupPath} : {exception.Message}");
            return null;
        }
    }

    public void Save(SaveData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string temporaryPath = $"{_path}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data, JsonOptions));
        File.Move(temporaryPath, _path, overwrite: true);
    }
}
