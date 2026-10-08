using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class StampPicker : VBoxContainer
{
    private static readonly Vector2 ButtonSize = new(34, 30);

    private readonly HFlowContainer _flow = new();
    private readonly ButtonGroup _group = new();
    private readonly Dictionary<string, Button> _buttons = [];
    private string _shown = string.Empty;

    public event Action<string>? Chosen;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        AddChild(WindowRows.Heading("Ton tampon"));
        AddChild(WindowRows.Muted("Tu le laisses dans le livre d'or des amis à qui tu rends visite. Si tu en changes, il change aussi dans tous leurs livres d'or."));
        _flow.AddThemeConstantOverride("h_separation", 6);
        _flow.AddThemeConstantOverride("v_separation", 6);
        AddChild(_flow);
    }

    public void Show(WorldData world, string chosen)
    {
        string shown = string.Join(",", world.Stamps.Select(stamp => stamp.Id));
        if (shown != _shown)
        {
            _shown = shown;
            Rebuild(world);
        }

        foreach ((string id, Button button) in _buttons)
        {
            button.SetPressedNoSignal(id == chosen);
        }
    }

    private void Rebuild(WorldData world)
    {
        WindowRows.Clear(_flow);
        _buttons.Clear();
        foreach (StampInfo stamp in world.Stamps)
        {
            Button button = new()
            {
                Icon = StampSprites.For(stamp.Id)?.Texture,
                TooltipText = stamp.Name,
                ToggleMode = true,
                ButtonGroup = _group,
                ExpandIcon = true,
                CustomMinimumSize = ButtonSize,
                FocusMode = FocusModeEnum.None,
                MouseDefaultCursorShape = CursorShape.PointingHand,
                TextureFilter = TextureFilterEnum.Nearest,
            };
            string id = stamp.Id;
            button.Pressed += () => Chosen?.Invoke(id);
            _buttons[id] = button;
            _flow.AddChild(button);
        }
    }
}
