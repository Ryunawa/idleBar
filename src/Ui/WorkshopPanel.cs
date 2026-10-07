using System;
using System.Globalization;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class WorkshopPanel : VBoxContainer, ITownPanel
{
    private const int MaxBatches = 99;

    private static readonly int[] BatchChoices = [1, 3, MaxBatches];
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private Label _status = null!;
    private ProgressBar _progress = null!;
    private VBoxContainer _recipes = null!;
    private Label _level = null!;
    private Button _upgrade = null!;
    private WorldData? _world;
    private GameSnapshot? _snapshot;
    private ServerClock? _clock;
    private int _batches = 1;

    public event Action<TownCommand>? Requested;


    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);

        _status = BarLabels.Create(14, BarPalette.Text);
        AddChild(_status);
        _progress = new ProgressBar { MaxValue = 1, Step = 0.001, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 6) };
        AddChild(_progress);
        AddChild(BatchPicker.Create("Fabrications par commande :", BatchChoices, MaxBatches, batches => _batches = batches));

        _recipes = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _recipes.AddThemeConstantOverride("separation", 8);
        AddChild(_recipes);

        HBoxContainer levelRow = new();
        _level = BarLabels.Create(12, BarPalette.Muted);
        _level.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _upgrade = new Button { FocusMode = FocusModeEnum.None };
        _upgrade.Pressed += () => Requested?.Invoke(actions => actions.UpgradeWorkshopAsync());
        levelRow.AddChild(_level);
        levelRow.AddChild(_upgrade);
        AddChild(levelRow);
    }

    public override void _Process(double delta)
    {
        if (IsVisibleInTree() && _world is not null && _snapshot?.Workshop is WorkshopState workshop && _clock is not null)
        {
            RefreshStatus(_world, workshop, _clock.Now);
        }
    }

    public void Refresh(TownContext context)
    {
        WorldData world = context.World;
        GameSnapshot snapshot = context.Snapshot;
        _world = world;
        _snapshot = snapshot;
        _clock = context.Clock;
        WorkshopState workshop = snapshot.Workshop!;

        ActionRow.Clear(_recipes);
        foreach (RecipeInfo recipe in world.RecipesOf(snapshot.Player!.CraftId))
        {
            _recipes.AddChild(CreateRecipeRow(context, workshop, recipe));
        }

        _level.Text = $"Atelier niveau {workshop.Level} · vitesse ×{workshop.Speed.ToString("0.##", French)} · entrepôt {workshop.StorageCapacity} · file de {workshop.MaxQueue}";
        _upgrade.Text = workshop.NextLevelPrice is int price ? $"Agrandir · {NumberFormat.Coins(price)}" : "Niveau maximal";
        _upgrade.Disabled = workshop.NextLevelPrice is not int cost || snapshot.Player.Coins < cost;
        RefreshStatus(world, workshop, context.Clock.Now);
    }

    private static string Describe(WorldData world, StockLine line) =>
        ExchangeText.Lot(world, line.GoodId, line.Quantity);

    private HBoxContainer CreateRecipeRow(TownContext context, WorkshopState workshop, RecipeInfo recipe)
    {
        WorldData world = context.World;
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 12);
        VBoxContainer details = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        details.AddThemeConstantOverride("separation", 0);

        string output = Describe(world, new StockLine(recipe.OutputGoodId, recipe.OutputQuantity));
        TimeSpan duration = TimeSpan.FromSeconds(recipe.Seconds / workshop.Speed);
        Label name = BarLabels.Create(14, BarPalette.Text);
        name.Text = $"{output} · {DurationFormat.Span(duration)}";
        details.AddChild(name);

        Label needs = BarLabels.Create(11, BarPalette.Muted);
        string inputs = string.Join(" + ", recipe.Inputs.Select(input => Describe(world, input)));
        string held = string.Join(", ", recipe.Inputs.Select(input => Describe(world, input with { Quantity = context.Owned(input.GoodId) })));
        needs.Text = $"Demande {inputs} · en stock : {held}";
        details.AddChild(needs);
        row.AddChild(details);

        bool sameRecipe = workshop.RecipeId == recipe.Id;
        Button start = new()
        {
            Text = sameRecipe ? "Ajouter" : "Lancer",
            Disabled = workshop.IsProducing && !sameRecipe,
            FocusMode = FocusModeEnum.None,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        start.Pressed += () => Requested?.Invoke(actions => actions.StartProductionAsync(recipe.Id, _batches));
        row.AddChild(start);
        return row;
    }

    private void RefreshStatus(WorldData world, WorkshopState workshop, DateTimeOffset now)
    {
        if (!workshop.IsProducing || world.FindRecipe(workshop.RecipeId!) is not RecipeInfo recipe)
        {
            _status.Text = "L'atelier est à l'arrêt : lance une fabrication.";
            _progress.Value = 0;
            return;
        }

        if (workshop.IsPaused(now))
        {
            _status.Text = $"Panne : l'atelier reprend à {DurationFormat.ClockTime(workshop.PausedUntil!.Value)}.";
            _progress.Value = 0;
            return;
        }

        int remaining = workshop.RemainingBatches(now);
        string output = Describe(world, new StockLine(recipe.OutputGoodId, remaining * recipe.OutputQuantity));
        _status.Text = $"En cours : {output} · fini à {DurationFormat.ClockTime(workshop.FinishesAt!.Value)}";
        _progress.Value = workshop.BatchProgress(now);
    }
}
