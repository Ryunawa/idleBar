using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed record TavernLayout(int Width, int DoorX, IReadOnlyList<int> StationXs, IReadOnlyList<int> SeatXs)
{
    public const int MinWidth = 180;
    public const int DoorWidth = 12;
    private const int FirstStation = 16;
    private const int StationGap = 21;
    private const int DoorGap = 8;
    private const int FirstSeatGap = 27;
    private const int SeatGap = 24;
    private const int MaxSeatGap = 110;
    private const int CompactSeatGap = 12;
    private const int EdgeGap = 10;
    private const int MinTail = 40;
    private const float TailShare = 0.22f;

    public int DoorCenter => DoorX + DoorWidth / 2;

    public static TavernLayout For(int width, int seats, int stations)
    {
        width = Math.Max(width, MinWidth);
        List<int> stationXs = Enumerable.Range(0, Math.Max(stations, 1)).Select(station => FirstStation + station * StationGap).ToList();
        int door = stationXs[^1] + DoorGap;
        int firstSeat = door + FirstSeatGap;
        int gap = SeatGap;
        if (seats > 1)
        {
            int roomy = (width - Math.Max(MinTail, (int)(width * TailShare)) - firstSeat) / (seats - 1);
            gap = roomy >= SeatGap
                ? Math.Min(roomy, MaxSeatGap)
                : Math.Clamp((width - EdgeGap - firstSeat) / (seats - 1), CompactSeatGap, SeatGap);
        }
        return new TavernLayout(
            width,
            door,
            stationXs,
            Enumerable.Range(0, seats).Select(seat => firstSeat + seat * gap).ToList());
    }
}
