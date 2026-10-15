using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class SkillNodeCard : PanelContainer
{
    private const int IconScale = 2;

    private static readonly Color Faded = new(1, 1, 1, 0.55f);

    private Label _title = null!;
    private Label _info = null!;
    private Label _effect = null!;
    private Button _learn = null!;

    public SkillNodeCard(SkillKind kind)
    {
        Kind = kind;
    }

    public event Action<SkillKind>? Learned;

    public SkillKind Kind { get; }

    public override void _Ready()
    {
        ThemeTypeVariation = WindowSkin.Card;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 2);
        HBoxContainer head = new();
        head.AddThemeConstantOverride("separation", 6);
        if (SceneSprites.SkillIcons.TryGetValue(Kind, out ArtSprite? icon))
        {
            head.AddChild(new TextureRect
            {
                Texture = SpriteTexture.From(icon),
                TextureFilter = TextureFilterEnum.Nearest,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(icon.Width * IconScale, icon.Height * IconScale),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            });
        }

        _title = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        head.AddChild(_title);
        rows.AddChild(head);
        _info = WindowRows.Muted(string.Empty);
        rows.AddChild(_info);
        _effect = WindowRows.Muted(string.Empty);
        rows.AddChild(_effect);
        _learn = new Button { SizeFlagsHorizontal = SizeFlags.Fill };
        _learn.Pressed += () => Learned?.Invoke(Kind);
        rows.AddChild(_learn);
        AddChild(rows);
    }

    public void Refresh(FrontierState state)
    {
        SkillInfo skill = Skills.Of(Kind);
        int level = SkillTree.Level(state, Kind);
        string? blocker = SkillTree.Blocker(state, Kind);
        _title.Text = skill.Name;
        _title.AddThemeColorOverride("font_color", level > 0 ? BarPalette.Gold : BarPalette.Text);
        string placed = SkillTree.RestartsIfPlaced(state, Kind) ? " · déjà placée dans la partie" : string.Empty;
        string wait = skill.Active && level > 0 ? $" · {SkillText.Short(state, Kind)}{placed}" : string.Empty;
        _info.Text = $"{(skill.Active ? "Active" : "Amélioration")} · {skill.Cost} point{(skill.Cost > 1 ? "s" : string.Empty)} · {level}/{skill.MaxLevel}{wait}";
        _effect.Text = skill.Active ? $"{SkillText.Now(state, Kind)} {Capitalized(SkillText.Recharge(state, Kind))}." : skill.Effect;
        _learn.Text = blocker ?? (level > 0 ? "Niveau suivant" : "Apprendre");
        _learn.Disabled = blocker is not null;
        _learn.TooltipText = blocker is null ? $"{skill.Name} : {skill.Cost} point{(skill.Cost > 1 ? "s" : string.Empty)}" : string.Empty;
        Modulate = level == 0 && blocker is not null ? Faded : Colors.White;
    }

    private static string Capitalized(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
