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

    public int DoorCenter => DoorX + DoorWidth / 2;

    public static TavernLayout For(int width, int seats, int stations)
    {
        List<int> stationXs = Enumerable.Range(0, Math.Max(stations, 1)).Select(station => FirstStation + station * StationGap).ToList();
        int door = stationXs[^1] + DoorGap;
        int firstSeat = door + FirstSeatGap;
        return new TavernLayout(
            Math.Max(width, MinWidth),
            door,
            stationXs,
            Enumerable.Range(0, seats).Select(seat => firstSeat + seat * SeatGap).ToList());
    }
}
