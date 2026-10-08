namespace IdleBar.Online;

public sealed record UpgradeInfo(string Id, string Kind, string Name, string Description, int Price, int Tier, string? Requires, string Value);
