using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed record TavernLayout(int Width, int DoorX, IReadOnlyList<int> StationXs, IReadOnlyList<int> SeatXs)
{
    public const int MinWidth = 180;
    public const int DoorWidth = 12;
    private const int TapX = 16;
    private const int TeapotX = 37;
    private const int DoorLeft = 45;
    private const int FirstSeat = 72;
    private const int SeatGap = 24;

    public int DoorCenter => DoorX + DoorWidth / 2;

    public static TavernLayout For(int width, int seats) =>
        new(
            Math.Max(width, MinWidth),
            DoorLeft,
            [TapX, TeapotX],
            Enumerable.Range(0, seats).Select(seat => FirstSeat + seat * SeatGap).ToList());
}
