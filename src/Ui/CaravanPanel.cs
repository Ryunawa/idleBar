using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class CaravanPanel : VBoxContainer
{
    private Label _summary = null!;
    private Label _cargo = null!;
    private Button _wagon = null!;

    public event Action? WagonRequested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 10);

        _summary = BarLabels.Create(14, BarPalette.Text);
        AddChild(_summary);

        _cargo = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _cargo.AddThemeColorOverride("font_color", BarPalette.Muted);
        AddChild(_cargo);

        _wagon = new Button { FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        _wagon.Pressed += () => WagonRequested?.Invoke();
        AddChild(_wagon);

        Label hint = BarLabels.Create(11, BarPalette.Muted);
        hint.Text = "Chaque chariot ajoute 20 places dans la cale. Les charrons de la ville les vendent au prix fort.";
        AddChild(hint);
    }

    public void Refresh(WorldData world, GameSnapshot snapshot)
    {
        CaravanState caravan = snapshot.Caravan!;
        PlayerState player = snapshot.Player!;
        string wagons = caravan.Wagons > 1 ? $"{caravan.Wagons} chariots" : "1 chariot";
        _summary.Text = $"Caravane de {player.Name} · {wagons} · cale {snapshot.HoldingsLoad}/{caravan.Capacity}";
        _cargo.Text = snapshot.Cargo.Count == 0
            ? "La cale est vide."
            : "Cargaison : " + string.Join(", ", snapshot.Cargo.Select(line => $"{world.GoodName(line.GoodId)} × {NumberFormat.Amount(line.Quantity)}"));

        if (caravan.NextWagonPrice is int price)
        {
            _wagon.Text = $"Ajouter un chariot · {NumberFormat.Amount(price)} écus";
            _wagon.Disabled = player.Coins < price;
            return;
        }

        _wagon.Text = "Nombre maximal de chariots atteint";
        _wagon.Disabled = true;
    }
}
