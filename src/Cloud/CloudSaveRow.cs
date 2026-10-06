using System;
using IdleBar.Game;

namespace IdleBar.Cloud;

internal sealed record CloudSaveRow(ProgressData Data, long Revision, string ActiveDevice, DateTimeOffset UpdatedAt);
