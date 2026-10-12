namespace IdleBar.Scene;

public sealed record LaneTarget(TargetKind Kind, int Index = -1)
{
    public static LaneTarget None { get; } = new(TargetKind.None);

    public bool Clickable => Kind is TargetKind.Chicken or TargetKind.Skill or TargetKind.Monster or TargetKind.Fighter or TargetKind.Building or TargetKind.BossCall;
}
