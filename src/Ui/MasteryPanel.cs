using System;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class MasteryPanel : VBoxContainer, ITownPanel
{
    private static readonly MasteryRank[] Tiers = [MasteryRank.Compagnon, MasteryRank.Maitre];

    private Label _title = null!;
    private ProgressBar _progress = null!;
    private Label _next = null!;
    private VBoxContainer _list = null!;

    public event Action<TownCommand>? Requested;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        _title = BarLabels.Create(15, BarPalette.Gold);
        AddChild(_title);
        _progress = new ProgressBar { MaxValue = 1, Step = 0.001, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 6) };
        AddChild(_progress);
        _next = ActionRow.Note(string.Empty);
        AddChild(_next);

        ScrollContainer scroll = new() { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_list);
        AddChild(scroll);
    }

    public void Refresh(TownContext context)
    {
        WorldData world = context.World;
        CraftInfo? craft = world.FindCraft(context.Player.CraftId);
        MasteryState? mastery = context.Snapshot.Mastery;
        ActionRow.Clear(_list);
        if (craft is null || mastery is null)
        {
            return;
        }

        int floor = mastery.Rank switch
        {
            MasteryRank.Maitre => world.Rules.MasterXp,
            MasteryRank.Compagnon => world.Rules.JourneymanXp,
            _ => 0,
        };
        _title.Text = $"{MasteryText.Title(mastery.Rank, craft.Name)} · {NumberFormat.Amount(mastery.Xp)} XP";
        _progress.Value = mastery.NextRankXp is int next ? (double)(mastery.Xp - floor) / Math.Max(next - floor, 1) : 1;
        string upcoming = mastery.NextRankXp is int threshold
            ? $"{MasteryText.RankName(mastery.Rank + 1)} à {NumberFormat.Amount(threshold)} XP. "
            : "Tu as atteint le plus haut rang. ";
        _next.Text = upcoming + MasteryText.HowToProgress(craft.Family);

        foreach (MasteryRank tier in Tiers)
        {
            AddTier(world, craft, mastery, tier);
        }

        AddMasterpieces(context);
    }

    private void AddTier(WorldData world, CraftInfo craft, MasteryState mastery, MasteryRank tier)
    {
        TalentInfo[] talents = world.Talents.Where(talent => talent.Family == craft.Family && talent.Tier == tier).ToArray();
        bool chosen = talents.Any(talent => mastery.Talents.Contains(talent.Id));
        bool reached = mastery.Rank >= tier;
        _list.AddChild(ActionRow.Heading($"Talent de {MasteryText.RankName(tier).ToLowerInvariant()}{(reached ? string.Empty : " · rang pas encore atteint")}"));
        foreach (TalentInfo talent in talents)
        {
            bool mine = mastery.Talents.Contains(talent.Id);
            string talentId = talent.Id;
            _list.AddChild(ActionRow.Create(mine ? $"{talent.Name} · choisi" : talent.Name, talent.Description, chosen ? string.Empty : "Choisir", !reached,
                () => Requested?.Invoke(actions => actions.ChooseTalentAsync(talentId))));
        }
    }

    private void AddMasterpieces(TownContext context)
    {
        if (context.Snapshot.Masterpieces.Count == 0)
        {
            return;
        }

        WorldData world = context.World;
        _list.AddChild(ActionRow.Heading("Chefs-d'œuvre"));
        foreach (MasterpieceInfo piece in context.Snapshot.Masterpieces)
        {
            long pieceId = piece.Id;
            string title = $"{world.GoodName(piece.GoodId)} de {piece.Maker}";
            string detail = $"Fait le {piece.CreatedAt.ToLocalTime():dd/MM} · à {world.TownName(piece.TownId)}";
            _list.AddChild(ActionRow.Create(title, detail, $"Vendre · {NumberFormat.Amount(piece.Price)} écus", piece.TownId != context.TownId,
                () => Requested?.Invoke(actions => actions.SellMasterpieceAsync(pieceId))));
        }
    }
}
