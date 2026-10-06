using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using IdleBar.Trade;

namespace IdleBar.Ui;

public partial class FoundingWindow : Window
{
    private const int MaxNameLength = 20;

    private static readonly Vector2I BaseSize = new(420, 400);
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private readonly List<CraftInfo> _crafts = [];
    private readonly List<TownInfo> _towns = [];
    private WorldData? _world;
    private LineEdit _name = null!;
    private OptionButton _craft = null!;
    private Label _craftDescription = null!;
    private OptionButton _town = null!;
    private Label _townHint = null!;
    private Button _submit = null!;
    private Label _message = null!;
    private bool _busy;

    public event Action<string, string, string>? Submitted;

    public override void _Ready()
    {
        VBoxContainer form = WindowFrame.Build(this, "IdleBar · S'installer", 8);

        form.AddChild(new Label
        {
            Text = "Choisis ton nom, ton métier et la ville où tu t'installes. Les autres joueurs verront ton nom au comptoir.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        _name = new LineEdit { PlaceholderText = "Ton nom de marchand", MaxLength = MaxNameLength };
        form.AddChild(_name);

        _craft = new OptionButton();
        _craft.ItemSelected += _ => RefreshHints();
        form.AddChild(_craft);
        _craftDescription = CreateHint();
        form.AddChild(_craftDescription);

        _town = new OptionButton();
        _town.ItemSelected += _ => RefreshHints();
        form.AddChild(_town);
        _townHint = CreateHint();
        form.AddChild(_townHint);

        _submit = new Button { Text = "S'installer" };
        _submit.Pressed += Submit;
        form.AddChild(_submit);

        _message = WindowFrame.CreateMessage();
        form.AddChild(_message);
    }

    public void Open(float scale, WorldData world)
    {
        _world = world;
        _crafts.Clear();
        _crafts.AddRange(world.Crafts);
        _towns.Clear();
        _towns.AddRange(world.Towns);

        _craft.Clear();
        foreach (CraftInfo craft in _crafts)
        {
            _craft.AddItem(craft.Playable ? craft.Name : $"{craft.Name} (bientôt)");
            _craft.SetItemDisabled(_craft.ItemCount - 1, !craft.Playable);
        }

        _craft.Select(_crafts.FindIndex(craft => craft.Playable));
        _message.Text = string.Empty;
        SetBusy(false);
        RefreshHints();
        WindowFrame.Present(this, BaseSize, scale);
        _name.GrabFocus();
    }

    public void ShowError(string message) => _message.Text = message;

    public void SetBusy(bool busy)
    {
        _busy = busy;
        _submit.Disabled = busy;
    }

    private static Label CreateHint()
    {
        Label hint = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        hint.AddThemeColorOverride("font_color", BarPalette.Muted);
        hint.AddThemeFontSizeOverride("font_size", 12);
        return hint;
    }

    private void RefreshHints()
    {
        if (_world is null || _craft.Selected < 0)
        {
            return;
        }

        CraftInfo craft = _crafts[_craft.Selected];
        _craftDescription.Text = craft.Description;
        HashSet<string> inputs = _world.RecipesOf(craft.Id).SelectMany(recipe => recipe.Inputs).Select(input => input.GoodId).ToHashSet();

        int selectedTown = Math.Max(_town.Selected, 0);
        _town.Clear();
        foreach (TownInfo town in _towns)
        {
            bool suited = town.Produces.Any(inputs.Contains);
            _town.AddItem(suited ? $"{town.Name} · matières premières sur place" : town.Name);
        }

        _town.Select(selectedTown);
        TownInfo chosen = _towns[selectedTown];
        List<string> local = chosen.Produces.Where(inputs.Contains).Select(goodId => _world.GoodName(goodId).ToLower(French)).ToList();
        _townHint.Text = craft.Kind switch
        {
            CraftKind.Itinerant => "Ta caravane partira de cette ville.",
            _ when craft.OpensBranches => "Ton comptoir sera ici. Tu pourras ouvrir des succursales dans d'autres villes.",
            _ when local.Count > 0 => $"Ton atelier restera ici. Bon marché sur place : {string.Join(", ", local)}.",
            _ => "Ton atelier restera ici, mais tes matières premières viennent d'ailleurs : elles coûteront plus cher.",
        };
    }

    private void Submit()
    {
        if (_busy || _craft.Selected < 0 || _town.Selected < 0)
        {
            return;
        }

        _message.Text = string.Empty;
        Submitted?.Invoke(_name.Text.Trim(), _crafts[_craft.Selected].Id, _towns[_town.Selected].Id);
    }
}
