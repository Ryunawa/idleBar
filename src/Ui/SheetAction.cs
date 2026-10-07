namespace IdleBar.Ui;

public sealed record SheetAction(string Label, bool Enabled, TownCommand Command, string Tooltip = "");