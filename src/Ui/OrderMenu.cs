using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Inn;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class OrderMenu : PopupMenu
{
    public const string Specialty = "specialty";

    private readonly List<string> _choices = [];

    public event Action<string>? Ordered;

    public override void _Ready()
    {
        PreferNativeMenu = false;
        IdPressed += id => Ordered?.Invoke(_choices[(int)id]);
    }

    public void Open(RoomData room, Vector2I point)
    {
        Clear();
        _choices.Clear();
        _choices.Add(Specialty);
        AddItem($"La spécialité : {room.Specialty.Name}", 0);
        foreach (string drink in room.Menu)
        {
            if (DrinkMenu.FromId(drink) is Drink known)
            {
                _choices.Add(drink);
                AddItem(DrinkMenu.Name(known), _choices.Count - 1);
            }
        }

        ResetSize();
        Popup(new Rect2I(point.X - Size.X / 2, point.Y - Size.Y - 8, Size.X, Size.Y));
    }
}
