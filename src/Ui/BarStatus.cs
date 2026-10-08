namespace IdleBar.Ui;

public sealed record BarStatus(string Headline, double? Coins, string Situation, string Compact, TipJarView? TipJar, VisitView? Visit = null);
