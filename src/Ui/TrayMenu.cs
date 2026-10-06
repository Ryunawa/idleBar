using System;
using Godot;

namespace IdleBar.Ui;

public partial class TrayMenu : Node
{
    private const int ToggleId = 0;
    private const int QuitId = 1;
    private const int SignOutId = 2;

    private PopupMenu _menu = null!;
    private StatusIndicator _indicator = null!;

    public event Action? ToggleRequested;

    public event Action? SignOutRequested;

    public event Action? QuitRequested;

    public override void _Ready()
    {
        _menu = new PopupMenu { PreferNativeMenu = true };
        _menu.AddItem("Replier", ToggleId);
        _menu.AddItem("Se déconnecter", SignOutId);
        _menu.AddSeparator();
        _menu.AddItem("Quitter", QuitId);
        _menu.IdPressed += OnItemPressed;
        AddChild(_menu);

        _indicator = new StatusIndicator { Icon = TrayIcon.Create(), Tooltip = "IdleBar" };
        _indicator.Pressed += (mouseButton, position) => OnIndicatorPressed((MouseButton)mouseButton, position);
        AddChild(_indicator);
    }

    public void SetCollapsed(bool collapsed) =>
        _menu.SetItemText(_menu.GetItemIndex(ToggleId), collapsed ? "Déplier" : "Replier");

    public void Refresh(string tooltip, bool signedIn)
    {
        _indicator.Tooltip = tooltip;
        _menu.SetItemDisabled(_menu.GetItemIndex(SignOutId), !signedIn);
    }

    public void Dismiss() => _indicator.Visible = false;

    private void OnIndicatorPressed(MouseButton button, Vector2I position)
    {
        if (button == MouseButton.Left)
        {
            ToggleRequested?.Invoke();
            return;
        }

        _menu.Popup(new Rect2I(position, Vector2I.Zero));
    }

    private void OnItemPressed(long id)
    {
        switch (id)
        {
            case ToggleId:
                ToggleRequested?.Invoke();
                break;
            case SignOutId:
                SignOutRequested?.Invoke();
                break;
            case QuitId:
                QuitRequested?.Invoke();
                break;
        }
    }
}
