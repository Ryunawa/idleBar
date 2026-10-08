using System;

namespace IdleBar.Online;

public sealed record StateData(DateTimeOffset ServerTime, TavernData? Tavern);
