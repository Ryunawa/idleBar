using System.Collections.Generic;

namespace IdleBar.Ui;

public sealed record BuildingSheet(string Title, string Summary, IReadOnlyList<SheetLine> Lines, IReadOnlyList<SheetAction> Actions, TownTab Tab);