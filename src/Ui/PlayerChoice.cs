using System;

namespace IdleBar.Ui;

public sealed record PlayerChoice(Guid Player, string Name, bool Self, bool Friend, bool Muted, long? Visit);
