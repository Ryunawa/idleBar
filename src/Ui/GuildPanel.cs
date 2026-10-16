using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class GuildPanel : VBoxContainer
{
    private SocialSession? _social;
    private VBoxContainer _lonely = null!;
    private LineEdit _name = null!;
    private LineEdit _code = null!;
    private Label _rejoin = null!;
    private Label _seals = null!;
    private VBoxContainer _guild = null!;
    private Label _title = null!;
    private HBoxContainer _chief = null!;
    private LineEdit _rename = null!;
    private Label _ready = null!;
    private VBoxContainer _members = null!;
    private Button _leave = null!;
    private Label _message = null!;
    private Label _loading = null!;
    private bool _busy;

    public override void _Ready()
    {
        Name = "Guilde";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 8);
        _seals = WindowRows.Heading(string.Empty);
        rows.AddChild(_seals);
        _lonely = new VBoxContainer();
        _lonely.AddThemeConstantOverride("separation", 8);
        _lonely.AddChild(WindowRows.Muted("Une guilde réunit jusqu'à 20 joueurs. Ses membres s'envoient des renforts et des convois, comptent ensemble leurs monstres contre le titan de la semaine et financent une recherche avec leurs sceaux. On la rejoint seulement avec son code."));
        _name = new LineEdit { PlaceholderText = "Nom de la guilde", MaxLength = 24, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _lonely.AddChild(Line(_name, "Fonder", () => Act(social => social.CreateGuildAsync(_name.Text), "Guilde fondée : partage son code avec tes amis.")));
        _code = new LineEdit { PlaceholderText = "Code de 6 caractères", MaxLength = 6, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _lonely.AddChild(Line(_code, "Rejoindre", () => Act(social => social.JoinGuildAsync(_code.Text), "Bienvenue dans la guilde !")));
        _rejoin = WindowRows.Muted(string.Empty);
        _lonely.AddChild(_rejoin);
        rows.AddChild(_lonely);

        _guild = new VBoxContainer();
        _guild.AddThemeConstantOverride("separation", 8);
        _title = new Label();
        _title.AddThemeColorOverride("font_color", BarPalette.Gold);
        HBoxContainer code = new();
        code.AddThemeConstantOverride("separation", 8);
        _title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        code.AddChild(_title);
        code.AddChild(Press("Copier le code", () => DisplayServer.ClipboardSet(_social?.Data?.Guild?.Code ?? string.Empty)));
        _guild.AddChild(code);
        _rename = new LineEdit { PlaceholderText = "Nouveau nom", MaxLength = 24, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _chief = Line(_rename, "Renommer", () => Act(social => social.RenameGuildAsync(_rename.Text), "Guilde renommée."));
        _chief.AddChild(Press("Nouveau code", () => Act(social => social.NewCodeAsync(), "Nouveau code : l'ancien ne marche plus.")));
        _guild.AddChild(_chief);
        _ready = WindowRows.Muted(string.Empty);
        _guild.AddChild(_ready);
        _members = new VBoxContainer();
        _members.AddThemeConstantOverride("separation", 4);
        _guild.AddChild(_members);
        _leave = Press("Quitter la guilde", Leave);
        _leave.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        _guild.AddChild(_leave);
        rows.AddChild(_guild);

        _loading = WindowRows.Muted("La guilde se charge…");
        rows.AddChild(_loading);
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
        if (_social?.Data is not SocialData data)
        {
            return;
        }

        double rejoin = _social.Left(data.RejoinIn);
        _rejoin.Text = rejoin > 0 ? $"Tu pourras rejoindre une guilde dans {GuildMessages.Wait(rejoin)}." : string.Empty;
        string seals = $"Sceaux : {data.Research?.Seals ?? 0}";
        _seals.Text = data.Guild is null ? $"{seals} · gardés pour ta future guilde" : $"{seals} · à verser dans l'onglet « Recherche »";
        if (data.Help is HelpData help)
        {
            double hero = _social.Left(help.HeroReadyIn);
            double convoy = _social.Left(help.ConvoyReadyIn);
            string heroText = hero > 0 ? $"Ton héros repart en renfort dans {GuildMessages.Wait(hero)}." : "Ton héros peut partir en renfort.";
            string convoyText = convoy > 0 ? $"Prochain convoi dans {GuildMessages.Wait(convoy)}." : "Un convoi est prêt.";
            _ready.Text = $"{heroText} {convoyText} Chaque aide se recharge en {ResearchText.Hours(GuildResearch.CooldownMinutes(data.ResearchLevels) / 60.0)}.";
        }
    }

    private void Rebuild()
    {
        if (!IsNodeReady())
        {
            return;
        }

        GuildData? guild = _social?.Data?.Guild;
        _lonely.Visible = _social?.Data is not null && guild is null;
        _guild.Visible = guild is not null;
        _loading.Visible = _social?.Data is null;
        WindowRows.Clear(_members);
        if (_social?.Data is null)
        {
            return;
        }

        if (guild is not null && _social.Data.Help is HelpData help)
        {
            _title.Text = $"{guild.Name} · code {guild.Code} · {guild.Members.Count}/20";
            _chief.Visible = guild.Leader;
            foreach (MemberData member in guild.Members)
            {
                MemberRow row = MemberRow.Create(member, guild.Leader, _social.Left(help.HeroReadyIn), _social.Left(help.ConvoyReadyIn), _social.Left(member.ConvoyIn), _social.Data.ResearchLevels);
                row.HelpRequested += kind => Act(social => social.SendHelpAsync(member.Id, kind), kind == SocialApi.HeroHelp
                    ? $"Ton héros combat {GuildResearch.AllyMinutes(_social.Data?.ResearchLevels)} min aux côtés de {member.Name}." : $"Convoi envoyé à {member.Name}.", true);
                row.KickRequested += () => Act(social => social.KickAsync(member.Id), $"{member.Name} ne fait plus partie de la guilde.");
                _members.AddChild(row);
            }
        }

        _leave.Text = "Quitter la guilde";
        Refresh();
    }

    private void Leave()
    {
        if (_leave.Text != "Confirmer le départ")
        {
            _leave.Text = "Confirmer le départ";
            return;
        }

        Act(social => social.LeaveGuildAsync(), "Tu as quitté la guilde.");
    }

    private async void Act(Func<SocialSession, Task<string>> action, string success, bool help = false)
    {
        if (_social is not SocialSession social || _busy)
        {
            return;
        }

        _busy = true;
        _message.Text = "…";
        string status;
        try
        {
            status = await action(social);
        }
        finally
        {
            _busy = false;
        }

        _message.Text = GuildMessages.Explain(status, social.Failure, success, help);
        if (status == SocialData.Ok)
        {
            _name.Text = string.Empty;
            _code.Text = string.Empty;
            _rename.Text = string.Empty;
        }
    }

    private static HBoxContainer Line(LineEdit field, string label, Action pressed)
    {
        HBoxContainer line = new();
        line.AddThemeConstantOverride("separation", 8);
        field.TextSubmitted += _ => pressed();
        line.AddChild(field);
        line.AddChild(Press(label, pressed));
        return line;
    }

    private static Button Press(string text, Action pressed)
    {
        Button button = new() { Text = text };
        button.Pressed += pressed;
        return button;
    }
}
