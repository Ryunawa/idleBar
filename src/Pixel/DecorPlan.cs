using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Pixel;

public sealed class DecorPlan
{
    private const int FireplaceSpacing = 260;
    private const int WindowEvery = 3;
    private const int PostGap = 30;

    private static readonly DecorKind[] WallKinds =
    [
        DecorKind.Window, DecorKind.Window, DecorKind.Window, DecorKind.Shelf, DecorKind.Shelf,
        DecorKind.Painting, DecorKind.Painting, DecorKind.Portrait, DecorKind.Trophy, DecorKind.Shield,
        DecorKind.Board, DecorKind.Banner, DecorKind.BlueBanner, DecorKind.Clock, DecorKind.Dartboard,
    ];

    private static readonly DecorKind[] FloorKinds = [DecorKind.Barrels, DecorKind.Barrels, DecorKind.Plant, DecorKind.Plant, DecorKind.SleepingCat, DecorKind.Fireplace];

    private static readonly DecorKind[] HangingKinds = [DecorKind.Lantern, DecorKind.Lantern, DecorKind.Lantern, DecorKind.Lantern, DecorKind.Garlic, DecorKind.Herbs, DecorKind.Pot];

    private static readonly DecorKind[] CounterKinds = [DecorKind.Candle, DecorKind.Candle, DecorKind.Bowl, DecorKind.Mug];

    private DecorPlan(int width, int wallStart, int floorStart)
    {
        Width = width;
        WallStart = wallStart;
        FloorStart = floorStart;
        Floor = PlanFloor(width, floorStart);
        List<int> posts = [];
        List<PlacedDecor> hanging = [new PlacedDecor(DecorKind.Lantern, wallStart - 4)];
        Wall = PlanWall(width, wallStart, floorStart, Floor.Where(piece => piece.Kind == DecorKind.Fireplace).Select(piece => piece.X).ToList(), posts, hanging);
        Posts = posts;
        Hanging = hanging;
        Counter = Scatter(width, floorStart + 20, CounterKinds, 70, 170, 4);
    }

    public int Width { get; }

    public int WallStart { get; }

    public int FloorStart { get; }

    public IReadOnlyList<PlacedDecor> Wall { get; }

    public IReadOnlyList<PlacedDecor> Floor { get; }

    public IReadOnlyList<PlacedDecor> Hanging { get; }

    public IReadOnlyList<PlacedDecor> Counter { get; }

    public IReadOnlyList<int> Posts { get; }

    public static DecorPlan For(int width, int wallStart, int floorStart) => new(width, wallStart, floorStart);

    private static List<PlacedDecor> PlanFloor(int width, int start)
    {
        Random random = new(1);
        List<PlacedDecor> pieces = [];
        int lastFireplace = -FireplaceSpacing;
        for (int x = start + random.Next(4, 20); x < width - DecorSizes.Fireplace;)
        {
            DecorKind kind = FloorKinds[random.Next(FloorKinds.Length)];
            if (kind == DecorKind.Fireplace && x - lastFireplace < FireplaceSpacing)
            {
                kind = DecorKind.Barrels;
            }

            lastFireplace = kind == DecorKind.Fireplace ? x : lastFireplace;
            pieces.Add(new PlacedDecor(kind, x));
            x += DecorSizes.Width(kind) + random.Next(80, 180);
        }

        return pieces;
    }

    private static List<PlacedDecor> PlanWall(int width, int wallStart, int floorStart, IReadOnlyList<int> fireplaces, List<int> posts, List<PlacedDecor> hanging)
    {
        Random random = new(2);
        List<PlacedDecor> pieces = [];
        int sinceWindow = 0;
        for (int x = wallStart; x < width - 6;)
        {
            int blocked = fireplaces.FirstOrDefault(fireplace => x + 14 > fireplace - 4 && x < fireplace + DecorSizes.Fireplace + 4, -1);
            if (blocked >= 0)
            {
                x = blocked + DecorSizes.Fireplace + 6;
                continue;
            }

            DecorKind kind = sinceWindow >= WindowEvery ? DecorKind.Window : PickFresh(random, pieces);
            sinceWindow = kind == DecorKind.Window ? 0 : sinceWindow + 1;
            pieces.Add(new PlacedDecor(kind, x));
            int gap = random.Next(22, 56);
            int middle = x + DecorSizes.Width(kind) + gap / 2;
            if (gap >= PostGap && random.NextDouble() < 0.3)
            {
                posts.Add(middle - 1);
            }
            else if (middle > floorStart && random.NextDouble() < 0.3)
            {
                DecorKind item = HangingKinds[random.Next(HangingKinds.Length)];
                hanging.Add(new PlacedDecor(item, middle - PropSprites.For(item).Width / 2));
            }

            x += DecorSizes.Width(kind) + gap;
        }

        return pieces;
    }

    private static DecorKind PickFresh(Random random, List<PlacedDecor> pieces)
    {
        while (true)
        {
            DecorKind kind = WallKinds[random.Next(WallKinds.Length)];
            if (pieces.TakeLast(2).All(piece => piece.Kind != kind))
            {
                return kind;
            }
        }
    }

    private static List<PlacedDecor> Scatter(int width, int start, DecorKind[] kinds, int minGap, int maxGap, int seed)
    {
        Random random = new(seed);
        List<PlacedDecor> pieces = [];
        for (int x = start + random.Next(minGap); x < width - 8; x += random.Next(minGap, maxGap))
        {
            pieces.Add(new PlacedDecor(kinds[random.Next(kinds.Length)], x));
        }

        return pieces;
    }
}
