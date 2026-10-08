using System;

namespace IdleBar.Ui;

public sealed record JournalEntry(DateTimeOffset At, string Text);
