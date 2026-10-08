using System;
using System.Linq;
using Godot;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class StampPicker : VBoxContainer
{
    private static readonly Vector2 ButtonSize = new(34, 30);

    private readonly HFlowContainer _flow = new();
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
        string shown = $"{chosen}|{string.Join(",", world.Stamps.Select(stamp => stamp.Id))}";
        if (shown == _shown)
        {
            return;
        }

        _shown = shown;
        WindowRows.Clear(_flow);
        foreach (StampInfo stamp in world.Stamps)
        {
            Button button = new()
            {
                Icon = StampSprites.For(stamp.Id)?.Texture,
                TooltipText = stamp.Name,
                ToggleMode = true,
                ButtonPressed = stamp.Id == chosen,
                ExpandIcon = true,
                CustomMinimumSize = ButtonSize,
                FocusMode = FocusModeEnum.None,
                MouseDefaultCursorShape = CursorShape.PointingHand,
                TextureFilter = TextureFilterEnum.Nearest,
            };
            string id = stamp.Id;
            button.Pressed += () => Chosen?.Invoke(id);
            _flow.AddChild(button);
        }
    }
}
