using System;
using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class TavernWindow : Window
{
    private static readonly Vector2I BaseSize = new(540, 430);

    private UpgradesPanel _upgrades = null!;
    private GoalsPanel _goals = null!;
    private TavernPanel _tavern = null!;
    private Label _message = null!;

    public event Action<string>? BuyRequested;

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · Ta taverne", 6);
        TabContainer tabs = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _upgrades = new UpgradesPanel();
        _upgrades.BuyRequested += id => BuyRequested?.Invoke(id);
        _goals = new GoalsPanel();
        _tavern = new TavernPanel();
        tabs.AddChild(_upgrades);
        tabs.AddChild(_goals);
        tabs.AddChild(_tavern);
        content.AddChild(tabs);
        _message = WindowFrame.CreateMessage();
        content.AddChild(_message);
    }

    public void Open(float scale)
    {
        _message.Text = string.Empty;
        WindowFrame.Present(this, BaseSize, scale);
    }

    public void Refresh(WorldData world, TavernData tavern, long coins)
    {
        Title = $"IdleBar · {tavern.Name}";
        _upgrades.Refresh(world, tavern, coins);
        _goals.Refresh(tavern);
        _tavern.Refresh(world, tavern);
    }

    public void ShowError(string message) => _message.Text = message;
}
