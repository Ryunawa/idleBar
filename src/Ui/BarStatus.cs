namespace IdleBar.Ui;

public sealed record BarStatus(string Coins, string Situation, string Compact, SlotContent Slot, LaneScene Scene, SlotContent? News = null);
