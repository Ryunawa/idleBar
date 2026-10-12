using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record LoadPlan(FrontierState State, double Away, bool Unsent);
