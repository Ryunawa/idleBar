using System.Linq;
using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class TavernPanel : VBoxContainer
{
    private VBoxContainer _rows = null!;

    public override void _Ready()
    {
        Name = "Taverne";
        _rows = new VBoxContainer();
        _rows.AddThemeConstantOverride("separation", 6);
        AddChild(WindowRows.Scroll(_rows));
    }

    public void Refresh(WorldData world, TavernData tavern)
    {
        WindowRows.Clear(_rows);
        TierInfo tier = world.Tiers.First(each => each.Tier == tavern.Tier);
        TierInfo? next = world.Tiers.FirstOrDefault(each => each.Tier == tavern.Tier + 1);
        _rows.AddChild(WindowRows.Heading($"{tavern.Name} · {tier.Name}"));
        VBoxContainer renown = new();
        renown.AddChild(new Label { Text = $"Renommée : {NumberFormat.Amount(tavern.Renown)}" });
        if (next is not null)
        {
            renown.AddChild(new ProgressBar
            {
                MinValue = tier.Renown,
                MaxValue = next.Renown,
                Value = tavern.Renown,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, 10),
            });
            renown.AddChild(WindowRows.Muted(
                $"Encore {NumberFormat.Amount(next.Renown - tavern.Renown)} pour devenir « {next.Name} ». Chaque client servi rapporte 1, et 1 de plus si le service est parfait."));
        }
        else
        {
            renown.AddChild(WindowRows.Muted("Ta taverne a atteint le plus haut rang."));
        }

        _rows.AddChild(WindowRows.Card(renown));
        VBoxContainer counts = new();
        counts.AddChild(new Label { Text = $"Clients servis : {NumberFormat.Amount(tavern.Served)}" });
        counts.AddChild(new Label { Text = $"Services parfaits : {NumberFormat.Amount(tavern.Perfect)}" });
        counts.AddChild(WindowRows.Muted(tavern.TipJar.Hourly > 0
            ? $"Pot à pourboires : {tavern.TipJar.Hourly} écus par heure d'absence, jusqu'à {tavern.TipJar.Cap}."
            : "Pot à pourboires : embauche une aide pour qu'il se remplisse pendant ton absence."));
        _rows.AddChild(WindowRows.Card(counts));
        VBoxContainer code = new();
        code.AddChild(new Label { Text = $"Code ami : {tavern.FriendCode}" });
        code.AddChild(WindowRows.Muted("Donne-le à tes amis pour qu'ils viennent boire un verre chez toi."));
        _rows.AddChild(WindowRows.Card(code));
    }
}
