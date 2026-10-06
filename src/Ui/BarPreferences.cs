using Godot;

namespace IdleBar.Ui;

public sealed class BarPreferences
{
    private const string Section = "bar";
    private const string CollapsedKey = "collapsed";

    private readonly string _path;

    private BarPreferences(string path, bool collapsed)
    {
        _path = path;
        Collapsed = collapsed;
    }

    public bool Collapsed { get; set; }

    public static BarPreferences Load(string path)
    {
        ConfigFile file = new();
        bool collapsed = file.Load(path) == Error.Ok && file.GetValue(Section, CollapsedKey, false).AsBool();
        return new BarPreferences(path, collapsed);
    }

    public void Save()
    {
        ConfigFile file = new();
        file.SetValue(Section, CollapsedKey, Collapsed);
        Error result = file.Save(_path);
        if (result != Error.Ok)
        {
            GD.PushWarning($"Préférences non enregistrées dans {_path} : {result}");
        }
    }
}
