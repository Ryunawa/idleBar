using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class TitanPanel : VBoxContainer
{
    private static readonly string[] Names = ["Colosse de pierre", "Hydre des marais", "Wyrm de givre", "Roi des sables"];
    private static readonly (long Monsters, string Reward)[] Tiers =
    [
        (200, "+25 % d'or et de dégâts pendant 2 h"), (1_000, "+50 % pendant 4 h"), (5_000, "+100 % pendant 8 h"),
    ];

    private SocialSession? _social;
    private Label _title = null!;
    private ProgressBar _life = null!;
    private Label _status = null!;
    private Label _share = null!;
    private Button _claim = null!;
    private VBoxContainer _ranking = null!;
    private Label _message = null!;
    private bool _busy;

    public override void _Ready()
    {
        Name = "Titan";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 8);
        rows.AddChild(WindowRows.Muted("Chaque semaine, un titan menace toutes les frontières. Chaque monstre que tu vaincs le frappe, 40 par minute au plus, et chaque joueur peut lui retirer au plus la moitié de ses PV. S'il tombe avant lundi, chacun reçoit une récompense selon sa propre part."));
        _title = new Label();
        _title.AddThemeColorOverride("font_color", BarPalette.Gold);
        rows.AddChild(_title);
        _life = new ProgressBar { MinValue = 0, MaxValue = 1, Step = 0.001, ShowPercentage = true, CustomMinimumSize = new Vector2(0, 18) };
        rows.AddChild(_life);
        _status = WindowRows.Muted(string.Empty);
        rows.AddChild(_status);
        _share = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        rows.AddChild(_share);
        _claim = new Button { SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        _claim.Pressed += Claim;
        rows.AddChild(_claim);
        rows.AddChild(WindowRows.Heading("Classement des guildes"));
        _ranking = new VBoxContainer();
        rows.AddChild(_ranking);
        _message = WindowFrame.CreateMessage();
        rows.AddChild(_message);
        AddChild(WindowRows.Scroll(rows));
        Rebuild();
    }

    public void Connect(SocialSession social)
    {
        _social = social;
        social.Changed += Rebuild;
        Rebuild();
    }

    public void Refresh()
    {
        if (_social?.Data?.Titan is TitanData titan)
        {
            string left = titan.Fallen ? "Abattu ! Rendez-vous lundi pour le suivant." : $"Il reste {GuildMessages.Wait(_social.Left(titan.EndsIn))} pour l'abattre.";
            _status.Text = $"{NumberFormat.Exact(titan.Damage)} / {NumberFormat.Exact(titan.Hp)} monstres · {left}";
        }
    }

    private void Rebuild()
    {
        if (!IsNodeReady())
        {
            return;
        }

        WindowRows.Clear(_ranking);
        if (_social?.Data?.Titan is not TitanData titan)
        {
            _title.Text = "Le titan se cherche…";
            _claim.Visible = false;
            return;
        }

        _title.Text = Names[titan.Kind % Names.Length];
        _life.Value = titan.Hp > 0 ? (double)titan.Damage / titan.Hp : 0;
        _share.Text = Share(titan);
        _claim.Visible = titan.Claimable is not null;
        _claim.Text = titan.Claimable is TitanReward reward ? $"Réclamer : +{reward.Percent} % d'or et de dégâts pendant {reward.Hours} h" : string.Empty;
        if (titan.Ranking.Count == 0)
        {
            _ranking.AddChild(WindowRows.Muted("Aucune guilde n'a encore frappé cette semaine."));
        }

        foreach (RankData rank in titan.Ranking)
        {
            Label line = new() { Text = $"{rank.Rank}. {rank.Name} · {NumberFormat.Exact(rank.Monsters)} monstres" };
            if (rank.Mine)
            {
                line.AddThemeColorOverride("font_color", BarPalette.Gold);
            }

            _ranking.AddChild(line);
        }

        Refresh();
    }

    private static string Share(TitanData titan)
    {
        string reached = "aucune récompense pour l'instant";
        string next = string.Empty;
        foreach ((long monsters, string reward) in Tiers)
        {
            if (titan.Mine >= monsters)
            {
                reached = reward;
            }
            else if (next.Length == 0)
            {
                next = $" Prochain palier à {NumberFormat.Exact(monsters)} monstres.";
            }
        }

        return $"Ta part : {NumberFormat.Exact(titan.Mine)} monstres, soit {reached} s'il tombe.{next}";
    }

    private async void Claim()
    {
        if (_social is not SocialSession social || _busy)
        {
            return;
        }

        _busy = true;
        string status = await social.ClaimTitanAsync();
        _busy = false;
        _message.Text = GuildMessages.For(status, "Récompense reçue : elle commence maintenant.");
    }
}
