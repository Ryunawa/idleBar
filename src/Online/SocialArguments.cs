namespace IdleBar.Online;

public sealed record GuildNameArguments(string PName);

public sealed record GuildCodeArguments(string PCode);

public sealed record MemberArguments(long PMember);

public sealed record HelpArguments(long PMember, string PKind);

public sealed record ItemArguments(long PItem);

public sealed record SlotArguments(string PSlot);
