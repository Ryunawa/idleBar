using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class GoalsPanel : VBoxContainer
{
    private VBoxContainer _rows = null!;

    public override void _Ready()
    {
        Name = "Objectifs";
        _rows = new VBoxContainer();
        _rows.AddThemeConstantOverride("separation", 6);
        AddChild(WindowRows.Scroll(_rows));
    }

    public void Refresh(TavernData tavern)
    {
        WindowRows.Clear(_rows);
        _rows.AddChild(WindowRows.Heading("Objectifs du jour"));
        _rows.AddChild(WindowRows.Muted("Trois petites demandes chaque jour. La récompense tombe dans ta bourse dès que l'objectif est rempli."));
        foreach (GoalData goal in tavern.Goals)
        {
            VBoxContainer content = new();
            content.AddThemeConstantOverride("separation", 2);
            HBoxContainer header = new();
            header.AddChild(new Label { Text = goal.Label, SizeFlagsHorizontal = SizeFlags.ExpandFill });
            Label reward = new() { Text = $"+{NumberFormat.Coins(goal.Reward)}" };
            reward.AddThemeColorOverride("font_color", goal.Done ? BarPalette.Success : BarPalette.Gold);
            header.AddChild(reward);
            content.AddChild(header);
            content.AddChild(new ProgressBar
            {
                MaxValue = goal.Target,
                Value = goal.Progress,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, 10),
            });
            content.AddChild(WindowRows.Muted(goal.Done ? "Rempli" : $"{goal.Progress} / {goal.Target}"));
            _rows.AddChild(WindowRows.Card(content));
        }
    }
}
