using Godot;
using IdleBar.Frontier;

namespace IdleBar.Ui;

public partial class RetreatPanel : VBoxContainer
{
    private const double ConfirmSeconds = 5;

    private Label _souls = null!;
    private Label _offer = null!;
    private Button _retreat = null!;
    private double _armed;

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Repli";
        AddThemeConstantOverride("separation", 8);
        AddChild(WindowRows.Muted("Se replier abandonne le terrain conquis : tu repars du niveau 1, sans or, sans bâtiments ni compagnons. En échange, tu gagnes des âmes, et chaque âme donne +2,5 % de dégâts et d'or pour toujours. Le premier repli débloque l'Intendant, qui achète à ta place."));
        _souls = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        AddChild(WindowRows.Card(_souls));
        _offer = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        AddChild(_offer);
        _retreat = new Button { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(260, 0) };
        _retreat.Pressed += OnPressed;
        AddChild(_retreat);
    }

    public override void _Process(double delta) => _armed = System.Math.Max(0, _armed - delta);

    public void Refresh()
    {
        if (Game is not FrontierGame game)
        {
            return;
        }

        FrontierState state = game.State;
        _souls.Text = $"Âmes : {state.Souls} · dégâts et or ×{Economy.SoulMultiplier(state.Souls):0.##} · {NumberFormat.Count(state.Retreats, "repli", "replis")} · record : niveau {state.BestStage}";
        int souls = RetreatRules.SoulsFor(state.MaxStage);
        if (souls < 1)
        {
            _offer.Text = $"Disponible à partir du niveau {Balance.RetreatStage}. Cette partie est allée jusqu'au niveau {state.MaxStage}.";
            _retreat.Text = "Se replier";
            _retreat.Disabled = true;
            return;
        }

        _offer.Text = $"Te replier maintenant rapporte {souls} âmes : dégâts et or ×{Economy.SoulMultiplier(state.Souls + souls):0.##}.";
        _retreat.Disabled = false;
        _retreat.Text = _armed > 0 ? "Confirmer : repartir du niveau 1" : $"Se replier : +{souls} âmes";
    }

    private void OnPressed()
    {
        if (_armed > 0)
        {
            _armed = 0;
            Game?.Retreat();
            Refresh();
            return;
        }

        _armed = ConfirmSeconds;
        Refresh();
    }
}
