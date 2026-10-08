using System;
using Godot;
using IdleBar.Online;

namespace IdleBar.Ui;

public partial class StreetMenu : PopupMenu
{
    private const int GreetId = 0;
    private const int InviteId = 1;

    private PasserbyData? _passerby;

    public event Action<PasserbyData>? GreetRequested;

    public event Action<PasserbyData>? InviteRequested;

    public override void _Ready()
    {
        PreferNativeMenu = false;
        IdPressed += OnPressed;
    }

    public void Open(PasserbyData passerby, Vector2I point)
    {
        _passerby = passerby;
        Clear();
        AddItem($"Saluer {passerby.Name}", GreetId);
        AddItem($"Inviter {passerby.Name} à boire un verre", InviteId);
        ResetSize();
        Popup(new Rect2I(point.X, point.Y - Size.Y - 8, Size.X, Size.Y));
    }

    private void OnPressed(long id)
    {
        if (_passerby is not PasserbyData passerby)
        {
            return;
        }

        if (id == GreetId)
        {
            GreetRequested?.Invoke(passerby);
        }
        else if (id == InviteId)
        {
            InviteRequested?.Invoke(passerby);
        }
    }
}
