using System.Collections.Generic;

namespace IdleBar.Online;

public sealed record RegularInfo(string Id, string Name, string Title, string Drink, string Condition, string Hint, string Souvenir, IReadOnlyList<ChapterInfo> Chapters);
