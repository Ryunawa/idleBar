using System;
using Godot;

namespace IdleBar.Ui;

public partial class PlayerMenu : PopupMenu
{
    private const int ProfileId = 0;
    private const int MuteId = 1;
    private const int DoorId = 2;
    private const int LeaveId = 3;

    private PlayerChoice? _choice;

    public event Action<Guid>? ProfileRequested;

    public event Action<Guid, bool>? MuteRequested;

    public event Action<long>? DoorRequested;

    public event Action? LeaveRequested;

    public override void _Ready()
    {
        PreferNativeMenu = false;
        IdPressed += OnPressed;
    }

    public void Open(PlayerChoice choice, Vector2I point)
    {
        _choice = choice;
        Clear();
        if (choice.Self)
        {
            AddItem("Rentrer dans ma taverne", LeaveId);
        }
        else
        {
            if (choice.Friend)
            {
                AddItem($"Voir la fiche de {choice.Name}", ProfileId);
            }

            AddItem(choice.Muted ? $"Afficher les messages de {choice.Name}" : $"Masquer les messages de {choice.Name}", MuteId);
            if (choice.Visit is not null)
            {
                AddItem($"Raccompagner {choice.Name} à la porte", DoorId);
            }
        }

        ResetSize();
        Popup(new Rect2I(point.X, point.Y - Size.Y - 8, Size.X, Size.Y));
    }

    private void OnPressed(long id)
    {
        if (_choice is not PlayerChoice choice)
        {
            return;
        }

        switch (id)
        {
            case ProfileId:
                ProfileRequested?.Invoke(choice.Player);
                break;
            case MuteId:
                MuteRequested?.Invoke(choice.Player, !choice.Muted);
                break;
            case DoorId when choice.Visit is long visit:
                DoorRequested?.Invoke(visit);
                break;
            case LeaveId:
                LeaveRequested?.Invoke();
                break;
        }
    }
}
