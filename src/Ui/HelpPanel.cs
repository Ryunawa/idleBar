using Godot;

namespace IdleBar.Ui;

public partial class HelpPanel : VBoxContainer
{
    public JournalPanel Journal { get; } = new();

    public override void _Ready()
    {
        Name = "Aide";
        TabContainer tabs = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        tabs.AddChild(Journal);
        tabs.AddChild(new FaqPanel());
        AddChild(tabs);
    }
}
