using System;
using Godot;

namespace IdleBar.Ui;

public static class ClickBinding
{
    public static void OnLeftPress(Control control, Action action)
    {
        control.GuiInput += inputEvent =>
        {
            if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                control.AcceptEvent();
                action();
            }
        };
    }
}
