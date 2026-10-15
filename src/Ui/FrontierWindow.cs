using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class FrontierWindow : Window
{
    private const double RefreshSeconds = 0.25;

    private static readonly Vector2I BaseSize = new(720, 470);

    private TabContainer _tabs = null!;
    private Label _wealth = null!;
    private FightersPanel _fighters = null!;
    private HeroPanel _hero = null!;
    private SkillsPanel _skills = null!;
    private LootPanel _loot = null!;
    private MarketPanel _market = null!;
    private VillagePanel _village = null!;
    private RetreatPanel _retreat = null!;
    private AchievementsPanel _achievements = null!;
    private GuildPanel _guild = null!;
    private TitanPanel _titan = null!;
    private HelpPanel _help = null!;
    private double _sinceRefresh;
    private FrontierGame? _game;

    public JournalPanel Journal => _help.Journal;

    public HeroPanel Hero => _hero;

    public FrontierGame? Game
    {
        get => _game;
        set
        {
            _game = value;
            if (IsNodeReady())
            {
                Bind();
            }
        }
    }

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · La Frontière", 6);
        HBoxContainer header = new();
        header.AddThemeConstantOverride("separation", 12);
        _wealth = BarLabels.Create(16, BarPalette.Gold);
        _wealth.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(_wealth);
        content.AddChild(header);
        _tabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _fighters = new FightersPanel();
        _hero = new HeroPanel();
        _skills = new SkillsPanel();
        _loot = new LootPanel();
        _market = new MarketPanel();
        _village = new VillagePanel();
        _retreat = new RetreatPanel();
        _achievements = new AchievementsPanel();
        _guild = new GuildPanel();
        _titan = new TitanPanel();
        _help = new HelpPanel();
        _tabs.AddChild(_fighters);
        _tabs.AddChild(_hero);
        _tabs.AddChild(_skills);
        _tabs.AddChild(_loot);
        _tabs.AddChild(_market);
        _tabs.AddChild(_village);
        _tabs.AddChild(_retreat);
        _tabs.AddChild(_guild);
        _tabs.AddChild(_titan);
        _tabs.AddChild(_achievements);
        _tabs.AddChild(_help);
        content.AddChild(_tabs);
        Bind();
    }

    public void Connect(SocialSession social)
    {
        _guild.Connect(social);
        _loot.Connect(social);
        _market.Connect(social);
        _titan.Connect(social);
    }

    public void Open(float scale, WindowTab tab)
    {
        _hero.Reset();
        _tabs.CurrentTab = (int)tab;
        Refresh();
        WindowFrame.Present(this, BaseSize, scale);
    }

    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        _sinceRefresh += delta;
        if (_sinceRefresh >= RefreshSeconds)
        {
            _sinceRefresh = 0;
            Refresh();
        }
    }

    private void Bind()
    {
        _fighters.Game = _game;
        _hero.Game = _game;
        _skills.Game = _game;
        _loot.Game = _game;
        _market.Game = _game;
        _village.Game = _game;
        _retreat.Game = _game;
        _achievements.Game = _game;
    }

    private void Refresh()
    {
        if (_game is not FrontierGame game)
        {
            return;
        }

        FrontierState state = game.State;
        _wealth.Text = $"{NumberFormat.Gold(state.Gold)} · +{NumberFormat.Rate(Economy.GoldPerSecond(state))}/s · niveau {state.Stage}";
        _fighters.Refresh();
        _loot.Refresh();
        _market.Refresh();
        _hero.Refresh();
        _skills.Refresh();
        _village.Refresh();
        _retreat.Refresh();
        _achievements.Refresh();
        _guild.Refresh();
        _titan.Refresh();
    }
}
