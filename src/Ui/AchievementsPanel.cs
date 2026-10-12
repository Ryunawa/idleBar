using System.Collections.Generic;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public partial class AchievementsPanel : VBoxContainer
{
    private readonly List<Label> _names = [];
    private Label _summary = null!;

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Succès";
        AddThemeConstantOverride("separation", 6);
        _summary = new Label();
        AddChild(_summary);
        VBoxContainer grid = new();
        grid.AddThemeConstantOverride("separation", 4);
        foreach (Achievement achievement in Achievements.All)
        {
            Label name = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            grid.AddChild(name);
            _names.Add(name);
        }

        AddChild(WindowRows.Scroll(grid));
    }

    public void Refresh()
    {
        if (Game is not FrontierGame game)
        {
            return;
        }

        HashSet<string> done = game.State.Achievements;
        _summary.Text = $"{done.Count} / {Achievements.All.Count} succès · +{done.Count * Balance.AchievementBonus * 100:0} % d'or et de dégâts";
        for (int index = 0; index < Achievements.All.Count; index++)
        {
            Achievement achievement = Achievements.All[index];
            bool reached = done.Contains(achievement.Id);
            _names[index].Text = reached ? $"{achievement.Name} · {achievement.Description}" : achievement.Description;
            _names[index].AddThemeColorOverride("font_color", reached ? BarPalette.Gold : BarPalette.Muted);
        }
    }
}
