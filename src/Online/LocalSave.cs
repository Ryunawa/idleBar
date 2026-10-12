using System;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record LocalSave(string PlayerId, int Revision, bool Unsent, DateTimeOffset WrittenAt, FrontierSave Save);
