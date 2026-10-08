using System.Linq;
using Godot;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class RegularsPanel : VBoxContainer
{
    private static readonly Vector2 PortraitSize = new(36, 52);
    private static readonly Color Unknown = new(0.05f, 0.04f, 0.03f, 0.9f);

    private VBoxContainer _rows = null!;

    public override void _Ready()
    {
        Name = "Habitués";
        _rows = new VBoxContainer();
        _rows.AddThemeConstantOverride("separation", 6);
        AddChild(WindowRows.Scroll(_rows));
    }

    public void Refresh(WorldData world, TavernData tavern)
    {
        WindowRows.Clear(_rows);
        _rows.AddChild(WindowRows.Heading($"Carnet des habitués · {tavern.Regulars.Count} sur {world.Regulars.Count}"));
        _rows.AddChild(WindowRows.Muted("Les habitués ont une bulle marquée d'un cœur. Bien les servir fait avancer leur histoire."));
        foreach (RegularInfo regular in world.Regulars)
        {
            _rows.AddChild(Card(regular, tavern.Regulars.FirstOrDefault(progress => progress.Id == regular.Id)));
        }
    }

    private static PanelContainer Card(RegularInfo regular, RegularProgress? progress)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        row.AddChild(Portrait(regular.Id, progress is not null));
        VBoxContainer text = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 2);
        text.AddChild(new Label { Text = progress is null ? "???" : $"{regular.Name}, {regular.Title}" });
        text.AddChild(WindowRows.Muted(regular.Hint));
        if (progress is not null)
        {
            ChapterInfo? next = regular.Chapters.FirstOrDefault(chapter => chapter.Chapter == progress.Chapter + 1);
            text.AddChild(new ProgressBar
            {
                MaxValue = next?.Friendship ?? 1,
                Value = next is null ? 1 : progress.Friendship,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, 8),
            });
            text.AddChild(WindowRows.Muted(next is null
                ? $"Amitié {progress.Friendship} · histoire terminée · souvenir : {regular.Souvenir}"
                : $"Amitié {progress.Friendship} · prochain chapitre à {next.Friendship}"));
            foreach (ChapterInfo chapter in regular.Chapters.Where(chapter => chapter.Chapter <= progress.Chapter))
            {
                text.AddChild(new Label { Text = $"« {chapter.Line} »", AutowrapMode = TextServer.AutowrapMode.WordSmart });
            }
        }

        row.AddChild(text);
        return WindowRows.Card(row);
    }

    private static TextureRect Portrait(string regular, bool known)
    {
        PatronLook look = RegularLooks.For(regular) ?? PatronLook.From(0);
        return new TextureRect
        {
            Texture = PatronSprites.For(look, Gaze.Front).Texture,
            CustomMinimumSize = PortraitSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            Modulate = known ? Colors.White : Unknown,
        };
    }
}
