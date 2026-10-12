using Godot;

namespace IdleBar.Ui;

public sealed class BarPreferences
{
    private const string Section = "bar";
    private const string CollapsedKey = "collapsed";
    private const string ScreenKey = "screen";

    private readonly string _path;

    private BarPreferences(string path)
    {
        _path = path;
    }

    public bool Collapsed { get; set; }

    public string? ScreenDevice { get; set; }

    public static BarPreferences Load(string path)
    {
        BarPreferences preferences = new(path);
        ConfigFile file = new();
        if (file.Load(path) != Error.Ok)
        {
            return preferences;
        }

        preferences.Collapsed = file.GetValue(Section, CollapsedKey, false).AsBool();
        string screen = file.GetValue(Section, ScreenKey, string.Empty).AsString();
        preferences.ScreenDevice = screen.Length == 0 ? null : screen;
        return preferences;
    }

    public void Save()
    {
        ConfigFile file = new();
        file.Load(_path);
        file.SetValue(Section, CollapsedKey, Collapsed);
        file.SetValue(Section, ScreenKey, ScreenDevice ?? string.Empty);
        Error result = file.Save(_path);
        if (result != Error.Ok)
        {
            GD.PushWarning($"Préférences non enregistrées dans {_path} : {result}");
        }
    }
}
