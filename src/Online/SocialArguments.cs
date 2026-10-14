namespace IdleBar.Online;

public sealed record GuildNameArguments(string PName);

public sealed record GuildCodeArguments(string PCode);

public sealed record MemberArguments(long PMember);

public sealed record HelpArguments(long PMember, string PKind);

public sealed record ItemArguments(long PItem);

public sealed record EquipArguments(long PItem, string PFighter);

public sealed record UnequipArguments(string PSlot, string PFighter);

public sealed record OfferArguments(System.Guid PSession, int PRevision, long POffer);

public sealed record UpgradeArguments(System.Guid PSession, int PRevision, long PItem, int PCount);
