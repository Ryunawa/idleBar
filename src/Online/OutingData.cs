using System;

namespace IdleBar.Online;

public sealed record OutingData(long Visit, string Host, DateTimeOffset StartedAt, DateTimeOffset? ServedAt, bool Perfect, bool Helped, string Specialty);
