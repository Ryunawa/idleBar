namespace IdleBar.Online;

public sealed record GoalData(string Id, string Label, int Progress, int Target, int Reward, bool Done);
