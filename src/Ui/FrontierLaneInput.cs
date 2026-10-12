using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class FrontierLane
{
    private Vector2? _mouse;
    private LaneTarget _hovered = LaneTarget.None;

    public event Action<WindowTab>? ManageRequested;

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseMotion motion:
                _mouse = motion.Position;
                Hover();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } button:
                _mouse = button.Position;
                Click();
                AcceptEvent();
                break;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit)
        {
            _mouse = null;
            _hovered = LaneTarget.None;
            HoveredSkill = -1;
            Hint = null;
        }
    }

    private void Hover()
    {
        if (Game is not FrontierGame game || ToArt() is not ArtPoint point)
        {
            return;
        }

        _hovered = LaneHits.Find(game.State, _layout, _scene, point.X, point.Y);
        HoveredSkill = _hovered.Kind == TargetKind.Skill ? _hovered.Index : -1;
        Hint = LaneHints.Describe(_hovered, game.State);
        MouseDefaultCursorShape = _hovered.Clickable ? CursorShape.PointingHand : CursorShape.Arrow;
    }

    private void Click()
    {
        Hover();
        if (Game is not FrontierGame game || ToArt() is not ArtPoint point)
        {
            return;
        }

        switch (_hovered.Kind)
        {
            case TargetKind.Chicken:
                game.CatchChicken();
                break;
            case TargetKind.Skill:
                game.UseSkill((SkillKind)_hovered.Index);
                break;
            case TargetKind.Monster:
                _scene.LastStrike = point;
                game.Strike();
                break;
            case TargetKind.Fighter:
                ManageRequested?.Invoke(WindowTab.Fighters);
                break;
            case TargetKind.Building:
                ManageRequested?.Invoke(WindowTab.Village);
                break;
            case TargetKind.BossCall:
                game.CallBoss();
                break;
        }
    }

    private ArtPoint? ToArt()
    {
        if (_mouse is not Vector2 mouse || _artScale <= 0)
        {
            return null;
        }

        float top = Size.Y - (FrontierLayout.Rows + _layout.Top) * _artScale;
        int x = (int)MathF.Floor(mouse.X / _artScale);
        int y = (int)MathF.Floor((mouse.Y - top) / _artScale) - _layout.Top;
        return new ArtPoint(x, y);
    }
}
