using Godot;

namespace IdleBar.Ui;

public partial class FaqWindow : Window
{
    private static readonly Vector2I BaseSize = new(560, 460);

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · Questions fréquentes", 6);
        content.AddChild(new FaqPanel { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
    }

    public void Open(float scale) => WindowFrame.Present(this, BaseSize, scale);
}
