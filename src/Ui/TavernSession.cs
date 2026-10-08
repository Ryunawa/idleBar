using System;
using Godot;
using IdleBar.Inn;

namespace IdleBar.Ui;

public sealed class TavernSession
{
    public const string Name = "Le Pot d'Étain";
    private const string SavePath = "user://tavern.cfg";
    private const string Section = "taverne";
    private const double SaveEverySeconds = 15;
    private const double LongestStep = 0.25;

    private double _sinceSave;

    public TavernSession()
    {
        ConfigFile file = new();
        file.Load(SavePath);
        Tavern = new Tavern(
            new Random(),
            file.GetValue(Section, "ecus", 0).AsInt64(),
            file.GetValue(Section, "servis", 0).AsInt32(),
            file.GetValue(Section, "parfaits", 0).AsInt32());
    }

    public Tavern Tavern { get; }

    public BarStatus Status => new(Tavern.Coins, Situation(), $"{Name} · {NumberFormat.Coins(Tavern.Coins)} · {Situation()}");

    public void Tick(double delta)
    {
        Tavern.Update((float)Math.Min(delta, LongestStep));
        _sinceSave += delta;
        if (_sinceSave >= SaveEverySeconds)
        {
            Save();
        }
    }

    public void Save()
    {
        _sinceSave = 0;
        ConfigFile file = new();
        file.SetValue(Section, "ecus", Tavern.Coins);
        file.SetValue(Section, "servis", Tavern.Served);
        file.SetValue(Section, "parfaits", Tavern.Perfect);
        file.Save(SavePath);
    }

    private string Situation() => Tavern.Waiting switch
    {
        0 => "Salle tranquille",
        1 => "1 client attend",
        int waiting => $"{waiting} clients attendent",
    };
}
