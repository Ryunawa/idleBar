using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class TitanPanel : VBoxContainer
{
    private SocialSession? _social;
    private Label _title = null!;
    private ProgressBar _life = null!;
    private Label _status = null!;
    private Label _share = null!;
    private Label _tiers = null!;
    private Button _claim = null!;
    private Label _last = null!;
    private VBoxContainer _ranking = null!;
    private Label _message = null!;
    private bool _busy;

    public override void _Ready()
    {
        Name = "Titan";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 8);
        rows.AddChild(WindowRows.Muted("Chaque lundi, un titan se dresse dans le brouillard de toutes les frontières. Il faut tous les joueurs pour l'abattre avant le lundi suivant : chaque monstre vaincu lui porte un coup, chaque boss dix, et le Cri de guerre de la recherche de guilde en ajoute. Personne ne peut lui retirer plus de la moitié de ses PV, ni plus de 60 coups par minute avant ce bonus."));
        _title = new Label();
        _title.AddThemeColorOverride("font_color", BarPalette.Gold);
        rows.AddChild(_title);
        _life = new ProgressBar { MinValue = 0, MaxValue = 1, Step = 0.001, ShowPercentage = true, CustomMinimumSize = new Vector2(0, 18) };
        rows.AddChild(_life);
        _status = WindowRows.Muted(string.Empty);
        rows.AddChild(_status);
        _share = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        rows.AddChild(_share);
        _tiers = WindowRows.Muted(string.Empty);
        rows.AddChild(_tiers);
        _claim = new Button { SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        _claim.Pressed += Claim;
        rows.AddChild(_claim);
        _last = WindowRows.Muted(string.Empty);
        rows.AddChild(_last);
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
            string left = titan.Fallen ? "Abattu ! Le prochain se dresse lundi." : $"Il reste {GuildMessages.Wait(_social.Left(titan.EndsIn))} pour l'abattre.";
            _status.Text = $"{NumberFormat.Exact(titan.Damage)} / {NumberFormat.Exact(titan.Hp)} coups · {titan.Fighters} héros dans la bataille · {left}";
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
            _title.Text = "Le titan se cherche dans le brouillard…";
            _claim.Visible = false;
            return;
        }

        IReadOnlyList<TitanTier> tiers = titan.Tiers ?? [];
        _title.Text = $"{Titans.Name(titan.Kind)} · titan de la semaine";
        _life.Value = titan.Hp > 0 ? 1 - (double)titan.Damage / titan.Hp : 0;
        _share.Text = Share(titan, tiers);
        _tiers.Text = "Paliers, si le titan tombe : " + string.Join(" ; ", tiers.Select(tier =>
            $"{NumberFormat.Exact(tier.Points)} coups, +{tier.Percent} % d'or et de dégâts pendant {tier.Hours} h{Seals(tier.Seals)}")) + ".";
        _claim.Visible = titan.Claimable is not null;
        _claim.Text = titan.Claimable is TitanReward reward ? $"Réclamer : +{reward.Percent} % d'or et de dégâts pendant {reward.Hours} h{Seals(reward.Seals)}" : string.Empty;
        _last.Text = titan.Last is LastTitan last
            ? $"La semaine dernière, le titan {Titans.Name(last.Kind)} {(last.Fallen ? "est tombé" : "a survécu")} ; ta part : {NumberFormat.Exact(last.Mine)} coups."
            : string.Empty;
        if (titan.Ranking.Count == 0)
        {
            _ranking.AddChild(WindowRows.Muted("Aucune guilde n'a encore frappé cette semaine."));
        }

        foreach (RankData rank in titan.Ranking)
        {
            Label line = new() { Text = $"{rank.Rank}. {rank.Name} · {NumberFormat.Exact(rank.Monsters)} coups" };
            if (rank.Mine)
            {
                line.AddThemeColorOverride("font_color", BarPalette.Gold);
            }

            _ranking.AddChild(line);
        }

        Refresh();
    }

    private static string Share(TitanData titan, IReadOnlyList<TitanTier> tiers)
    {
        TitanTier? reached = tiers.LastOrDefault(tier => titan.Mine >= tier.Points);
        TitanTier? next = tiers.FirstOrDefault(tier => titan.Mine < tier.Points);
        string reward = reached is null ? "pas encore de récompense" : $"+{reached.Percent} % pendant {reached.Hours} h{Seals(reached.Seals)} s'il tombe";
        string ahead = next is null ? string.Empty : $" Prochain palier à {NumberFormat.Exact(next.Points)} coups.";
        return $"Ta part : {NumberFormat.Exact(titan.Mine)} coups, {reward}.{ahead}";
    }

    private static string Seals(int seals) => seals > 0 ? $" et {ResearchSnapshot.Seal(seals)}" : string.Empty;

    private async void Claim()
    {
        if (_social is not SocialSession social || _busy)
        {
            return;
        }

        _busy = true;
        try
        {
            _message.Text = GuildMessages.Explain(await social.ClaimTitanAsync(), social.Failure, "Récompense reçue : elle commence maintenant.");
        }
        finally
        {
            _busy = false;
        }
    }
}
