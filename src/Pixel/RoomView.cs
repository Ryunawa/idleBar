using System.Collections.Generic;
using IdleBar.Inn;

namespace IdleBar.Pixel;

public sealed record RoomView(TavernLayout Layout, DecorSet Decor, IReadOnlyList<string> Souvenirs, Outdoors Outdoors, bool DoorOpen);
