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
        AddChild(WindowRows.Muted("Se replier abandonne le terrain conquis : tu repars du niveau 1, sans or, sans bâtiments ni compagnons. En échange, tu gagnes des âmes : chaque âme donne +2,5 % de dégâts et d'or pour toujours. Les âmes viennent de tout l'or gagné depuis le début, et chacune demande plus d'or que la précédente ; un repli ajoute celles gagnées depuis le dernier."));
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
        _souls.Text = $"Âmes : {state.Souls} · dégâts et or ×{NumberFormat.Factor(Economy.SoulMultiplier(state.Souls))} · {NumberFormat.Count(state.Retreats, "repli", "replis")} · record : niveau {state.BestStage}";
        int souls = RetreatRules.Pending(state);
        string next = $"Prochaine âme dans {NumberFormat.Gold(RetreatRules.GoldToNextSoul(state))} gagnés.";
        if (souls < 1)
        {
            _offer.Text = state.MaxStage < Balance.RetreatStage
                ? $"Disponible à partir du niveau {Balance.RetreatStage}. Cette partie est allée jusqu'au niveau {state.MaxStage}."
                : $"Aucune âme nouvelle depuis ton dernier repli. {next}";
            _retreat.Text = "Se replier";
            _retreat.Disabled = true;
            return;
        }

        _offer.Text = $"Te replier maintenant rapporte {NumberFormat.Count(souls, "âme", "âmes")} : dégâts et or ×{NumberFormat.Factor(Economy.SoulMultiplier(state.Souls + souls))}. {next}";
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
