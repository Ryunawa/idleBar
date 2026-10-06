using Godot;
using IdleBar.Cloud;

namespace IdleBar.Ui;

public sealed class SyncSlot
{
    private readonly Label _status;
    private CloudStatus? _shownStatus;
    private string? _shownDevice;

    public SyncSlot()
    {
        Button = new Button
        {
            CustomMinimumSize = new Vector2(104, 0),
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };

        VBoxContainer content = new()
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        content.AddThemeConstantOverride("separation", 0);
        content.AddChild(CreateLabel("Synchro", 12));
        _status = CreateLabel(string.Empty, 11);
        content.AddChild(_status);
        Button.AddChild(content);
        content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public Button Button { get; }

    public void Refresh(CloudStatus status, string? activeDevice)
    {
        if (status == _shownStatus && activeDevice == _shownDevice)
        {
            return;
        }

        _shownStatus = status;
        _shownDevice = activeDevice;
        string device = activeDevice ?? "un autre PC";

        _status.Text = status switch
        {
            CloudStatus.SignedOut => "Se connecter",
            CloudStatus.Connecting => "Connexion…",
            CloudStatus.Active => "Active",
            CloudStatus.Passive => $"Sur {device}",
            _ => "Hors ligne",
        };

        _status.AddThemeColorOverride("font_color", status switch
        {
            CloudStatus.Active => BarPalette.Success,
            CloudStatus.Passive => BarPalette.Warning,
            CloudStatus.Offline => BarPalette.Danger,
            _ => BarPalette.Muted,
        });

        Button.TooltipText = status switch
        {
            CloudStatus.SignedOut => "Synchronisation désactivée. Clique pour te connecter.",
            CloudStatus.Connecting => "Connexion à Supabase…",
            CloudStatus.Active => "Progression synchronisée depuis ce PC.",
            CloudStatus.Passive => $"La partie est active sur {device}. Clique pour la reprendre ici.",
            _ => "Supabase injoignable, la partie continue en local. Clique pour réessayer.",
        };
    }

    private static Label CreateLabel(string text, int fontSize)
    {
        Label label = new()
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipText = true,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }
}
