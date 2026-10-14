using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class ShopSection : VBoxContainer
{
    private readonly List<ShopRow> _rows = [];
    private Label _heading = null!;
    private Label _loading = null!;

    public event Action<OfferData>? Bought;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        _heading = WindowRows.Heading("Boutique");
        _loading = WindowRows.Muted("La boutique se charge…");
        AddChild(_heading);
        AddChild(_loading);
    }

    public void Show(ShopData? shop)
    {
        int count = shop?.Offers.Count ?? 0;
        while (_rows.Count < count)
        {
            ShopRow row = new();
            row.Bought += offer => Bought?.Invoke(offer);
            _rows.Add(row);
            AddChild(row);
        }

        for (int index = 0; index < _rows.Count; index++)
        {
            _rows[index].Visible = index < count;
            if (shop is not null && index < count)
            {
                _rows[index].Show(shop.Offers[index]);
            }
        }

        _loading.Visible = shop is null;
    }

    public void Refresh(double refreshIn, double gold, bool idle, bool room)
    {
        _heading.Text = $"Boutique · nouvel arrivage dans {GuildMessages.Wait(refreshIn)}";
        foreach (ShopRow row in _rows)
        {
            row.Refresh(gold, idle, room);
        }
    }
}
