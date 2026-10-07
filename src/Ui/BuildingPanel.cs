using System;
using System.Linq;
using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class BuildingPanel : Window
{
    private const int PanelWidth = 300;
    private const int Gap = 6;
    private const int IconSize = 24;

    private Label _summary = null!;
    private VBoxContainer _lines = null!;
    private HFlowContainer _actions = null!;
    private Label _message = null!;
    private Button _more = null!;
    private BuildingSheet? _sheet;
    private string _shown = string.Empty;
    private bool _busy;

    public event Action<TownCommand>? Requested;

    public event Action<TownTab>? MoreRequested;

    public StreetBuilding? Building { get; private set; }

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, string.Empty, 6);
        WrapControls = true;
        _summary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(PanelWidth, 0) };
        content.AddChild(_summary);
        _lines = new VBoxContainer();
        _lines.AddThemeConstantOverride("separation", 2);
        content.AddChild(_lines);
        _actions = new HFlowContainer();
        _actions.AddThemeConstantOverride("h_separation", 6);
        _actions.AddThemeConstantOverride("v_separation", 6);
        content.AddChild(_actions);
        _message = WindowFrame.CreateMessage();
        _message.Visible = false;
        content.AddChild(_message);
        _more = new Button { Text = "Tout voir ›", Flat = true, FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd };
        _more.Pressed += () =>
        {
            if (_sheet is { } sheet)
            {
                MoreRequested?.Invoke(sheet.Tab);
            }
        };
        content.AddChild(_more);
        FocusExited += Hide;
    }

    public void Open(StreetBuilding building, BuildingSheet sheet, Theme theme, Vector2I anchor, float scale)
    {
        Building = building;
        Theme = theme;
        ContentScaleFactor = scale;
        SetMessage(string.Empty);
        Refresh(sheet);
        Show();
        Place(anchor, scale);
        GrabFocus();
    }

    public void Refresh(BuildingSheet sheet)
    {
        _sheet = sheet;
        string shown = Describe(sheet);
        if (shown == _shown)
        {
            return;
        }

        _shown = shown;
        Title = sheet.Title;
        _summary.Text = sheet.Summary;
        ActionRow.Clear(_lines);
        foreach (SheetLine line in sheet.Lines)
        {
            _lines.AddChild(CreateLine(line));
        }

        ActionRow.Clear(_actions);
        foreach (SheetAction action in sheet.Actions)
        {
            Button button = new() { Text = action.Label, TooltipText = action.Tooltip, Disabled = !action.Enabled || _busy, FocusMode = Control.FocusModeEnum.None };
            TownCommand command = action.Command;
            button.Pressed += () => Requested?.Invoke(command);
            _actions.AddChild(button);
        }

        _actions.Visible = sheet.Actions.Count > 0;
        ResetSize();
    }

    public void SetBusy(bool busy)
    {
        _busy = busy;
        if (busy)
        {
            SetMessage(string.Empty);
        }

        if (_sheet is { } sheet)
        {
            Refresh(sheet);
        }
    }

    public void ShowError(string message) => SetMessage(message);

    private void SetMessage(string message)
    {
        _message.Text = message;
        _message.Visible = message.Length > 0;
        ResetSize();
    }

    private void Place(Vector2I anchor, float scale)
    {
        ResetSize();
        Vector2I size = Size;
        int screen = DisplayServer.GetScreenFromRect(new Rect2(anchor, Vector2.One));
        Rect2I usable = DisplayServer.ScreenGetUsableRect(screen < 0 ? DisplayServer.WindowGetCurrentScreen() : screen);
        int x = Math.Clamp(anchor.X - size.X / 2, usable.Position.X, Math.Max(usable.Position.X, usable.End.X - size.X));
        int y = Math.Max(anchor.Y - size.Y - (int)(Gap * scale), usable.Position.Y);
        Position = new Vector2I(x, y);
    }

    private string Describe(BuildingSheet sheet) =>
        string.Join("\n", [
            sheet.Title,
            sheet.Summary,
            .. sheet.Lines.Select(line => $"{line.GoodId}|{line.Color.ToHtml()}|{line.Text}"),
            .. sheet.Actions.Select(action => $"{action.Enabled && !_busy}|{action.Label}|{action.Tooltip}"),
        ]);

    private static HBoxContainer CreateLine(SheetLine line)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 6);
        if (line.GoodId is string goodId && GoodBadge.CreateIcon(goodId) is TextureRect icon)
        {
            icon.CustomMinimumSize = new Vector2(IconSize, IconSize);
            row.AddChild(icon);
        }

        Label text = BarLabels.Create(13, line.Color);
        text.Text = line.Text;
        text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        text.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        text.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        text.CustomMinimumSize = new Vector2(PanelWidth - IconSize - 6, 0);
        row.AddChild(text);
        return row;
    }
}