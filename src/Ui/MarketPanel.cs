using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class MarketPanel : VBoxContainer
{
    private SocialSession? _social;
    private ShopSection _shop = null!;
    private Label _message = null!;
    private bool _busy;

    public FrontierGame? Game { get; set; }

    public override void _Ready()
    {
        Name = "Marché";
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 6);
        rows.AddChild(WindowRows.Muted("La boutique propose quatre objets tirés au hasard, au niveau de ton front, et se renouvelle toutes les 3 heures. Le serveur fixe les prix et prend l'or de ta dernière sauvegarde ; un objet acheté rejoint ton sac et reste lié à toi."));
        _shop = new ShopSection();
        _shop.Bought += offer => Act(social => social.BuyAsync(offer), "Objet acheté : il t'attend dans le sac.");
        rows.AddChild(_shop);
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
        if (!IsNodeReady() || _social?.Data is not SocialData data || data.Shop is not ShopData shop)
        {
            return;
        }

        ItemsData? items = data.Items;
        bool room = items is not null && items.Carried < items.Max;
        bool idle = !_busy && Game is { Frozen: false } && items is not null;
        _shop.Refresh(_social.Left(shop.RefreshIn), Game?.State.Gold ?? 0, idle, room);
    }

    private void Rebuild()
    {
        if (!IsNodeReady())
        {
            return;
        }

        _shop.Show(_social?.Data?.Shop);
        Refresh();
    }

    private async void Act(Func<SocialSession, Task<string>> action, string success)
    {
        if (_social is not SocialSession social || _busy)
        {
            return;
        }

        _busy = true;
        _message.Text = "…";
        try
        {
            _message.Text = GuildMessages.Explain(await action(social), social.Failure, success);
        }
        finally
        {
            _busy = false;
        }
    }
}
