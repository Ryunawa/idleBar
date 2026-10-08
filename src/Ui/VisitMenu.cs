using System;
using Godot;

namespace IdleBar.Ui;

public partial class VisitMenu : PopupMenu
{
    private const int OrderId = 10;
    private const int LeaveId = 11;
    private static readonly string[] Emotes = ["cheers", "thanks", "laugh"];

    public event Action<string>? Emoted;

    public event Action? OrderRequested;

    public event Action? LeaveRequested;

    public override void _Ready()
    {
        PreferNativeMenu = false;
        for (int index = 0; index < Emotes.Length; index++)
        {
            AddItem(VisitDesk.Text(Emotes[index]), index);
        }

        AddSeparator();
        AddItem("Commander…", OrderId);
        AddItem("Rentrer dans ma taverne", LeaveId);
        IdPressed += OnPressed;
    }

    public void Open(Vector2I point)
    {
        ResetSize();
        Popup(new Rect2I(point.X - Size.X / 2, point.Y - Size.Y - 8, Size.X, Size.Y));
    }

    private void OnPressed(long id)
    {
        switch (id)
        {
            case OrderId:
                OrderRequested?.Invoke();
                break;
            case LeaveId:
                LeaveRequested?.Invoke();
                break;
            default:
                Emoted?.Invoke(Emotes[id]);
                break;
        }
    }
}
