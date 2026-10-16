using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class ResearchPanel : VBoxContainer
{
    private readonly List<ResearchNodeCard> _cards = [];
    private SocialSession? _social;
    private Label _seals = null!;
    private Label _lonely = null!;
    private Label _running = null!;
    private ResearchQueueList _queue = null!;
    private Label _message = null!;
    private bool _busy;

    public override void _Ready()
    {
        Name = "Recherche";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 8);
        rows.AddChild(WindowRows.Muted($"Chaque boss vaincu rapporte un sceau, {Researches.SealsPerHour} par heure au plus, et les récompenses du titan en donnent selon le palier. Les sceaux sont à toi, avec ou sans guilde, et ne servent qu'à la recherche de ta guilde. Chaque membre verse les siens au nœud de son choix ; un nœud demande le précédent de sa branche, recherché au moins au niveau 1. Un niveau financé part en recherche : une seule à la fois par guilde, dans l'ordre où les financements ont été complétés, que le chef peut changer. Le coût grandit avec le niveau et avec la guilde au-delà de {Researches.MembersStep} membres, et le prix d'un niveau est fixé au premier sceau versé. Les bonus valent pour tous les membres tant qu'ils restent dans la guilde, et les sceaux versés restent à la guilde."));
        _seals = WindowRows.Heading(string.Empty);
        rows.AddChild(_seals);
        _lonely = WindowRows.Muted("Tu n'as pas de guilde : tes sceaux t'attendent. Fonde ou rejoins une guilde dans l'onglet « Guilde » pour les verser à sa recherche.");
        rows.AddChild(_lonely);
        _running = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        rows.AddChild(_running);
        _queue = new ResearchQueueList();
        _queue.Moved += Move;
        rows.AddChild(_queue);
        _message = WindowFrame.CreateMessage();
        rows.AddChild(_message);
        rows.AddChild(Branches());
        AddChild(WindowRows.Scroll(rows));
        Refresh();
    }

    public void Connect(SocialSession social)
    {
        _social = social;
        social.Changed += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        if (!IsNodeReady())
        {
            return;
        }

        SocialData? data = _social?.Data;
        ResearchSnapshot snapshot = new(data?.Research, data?.Guild is not null && data.Research is not null,
            _social is null ? 0 : (DateTimeOffset.UtcNow - _social.ReceivedAt).TotalSeconds, data?.Guild?.Leader == true);
        _seals.Text = data is null ? "La recherche se charge…" : $"Sceaux : {snapshot.Seals}";
        _lonely.Visible = data is not null && data.Guild is null;
        _running.Visible = snapshot.Member;
        _queue.Visible = snapshot.Member;
        _running.Text = snapshot.Running is QueuedResearch running && running.Kind is ResearchKind kind
            ? $"En recherche : {Researches.Of(kind).Name} niveau {running.Level} · fin dans {GuildMessages.Wait(snapshot.Left(running.EndsIn))}"
            : "Aucune recherche en cours : finance un niveau pour en lancer une.";
        _queue.Refresh(snapshot);
        foreach (ResearchNodeCard card in _cards)
        {
            card.Refresh(snapshot);
        }
    }

    private HBoxContainer Branches()
    {
        HBoxContainer columns = new();
        columns.AddThemeConstantOverride("separation", 8);
        foreach (ResearchBranch branch in Researches.Branches)
        {
            VBoxContainer column = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            column.AddThemeConstantOverride("separation", 6);
            Label title = WindowRows.Heading(Researches.Name(branch));
            title.HorizontalAlignment = HorizontalAlignment.Center;
            column.AddChild(title);
            foreach (ResearchInfo research in Researches.In(branch))
            {
                ResearchNodeCard card = new(research.Kind);
                card.Funded += Fund;
                column.AddChild(card);
                _cards.Add(card);
            }

            columns.AddChild(column);
        }

        return columns;
    }

    private async void Fund(ResearchKind kind, int amount)
    {
        if (_social is not SocialSession social || _busy || amount < 1)
        {
            return;
        }

        _busy = true;
        _message.Text = "…";
        try
        {
            string status = await social.FundAsync(kind, amount);
            _message.Text = ResearchMessages.For(status, social.Failure, ResearchMessages.Thanks(social.LastGiven, Researches.Of(kind).Name));
        }
        finally
        {
            _busy = false;
        }
    }

    private async void Move(ResearchKind kind, int level, int offset)
    {
        if (_social is not SocialSession social || _busy)
        {
            return;
        }

        _busy = true;
        _message.Text = "…";
        try
        {
            string status = await social.MoveAsync(kind, level, offset);
            string success = $"« {Researches.Of(kind).Name} » niveau {level} {(offset < 0 ? "avance" : "recule")} d'une place dans la file.";
            _message.Text = ResearchMessages.ForMove(status, social.Failure, success);
        }
        finally
        {
            _busy = false;
        }
    }
}
