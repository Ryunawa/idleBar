using System.Collections.Generic;

namespace IdleBar.Frontier;

public sealed record Biome(string Name, IReadOnlyList<string> Monsters);
