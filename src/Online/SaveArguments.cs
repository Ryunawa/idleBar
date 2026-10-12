using System;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record SaveArguments(FrontierSave PSave, int PRevision, Guid PSession);
