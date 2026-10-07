using System.Collections.Generic;

namespace IdleBar.Pixel;

public sealed record StreetScene(StreetState Street, bool Busy, bool Errands, IReadOnlyList<string> Products, ProductPop Pop, CaravanLook? Look);