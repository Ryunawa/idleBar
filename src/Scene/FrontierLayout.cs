using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public sealed record FrontierLayout(int Width, int Top, int VillageX, int SlotWidth, int RoadWidth, int FrontX, int FogWidth, int SkillSize, bool Compact)
{
    public const int Rows = 28;
    public const int Ground = 24;
    public const int RoomyFront = 92;
    public const int CompactFront = 74;
    public const int Slots = FrontierState.PlotCount;

    private const int Margin = 2;
    private const int NarrowSlot = 16;
    private const int ComfortSlot = 32;
    private const int WideSlot = 40;
    private const int ComfortRoad = 60;
    private const int WideRoad = 120;
    private const int ComfortFog = 40;
    private const int WideFog = 80;
    private const int PalisadeRoad = 14;
    private const int CrierRoad = 28;

    public int FrontWidth => Compact ? CompactFront : RoomyFront;

    public bool Narrow => SlotWidth == 0;

    public int VillageEnd => VillageX + SlotWidth * Slots;

    public bool Palisade => !Narrow && RoadWidth >= PalisadeRoad;

    public int PalisadeX => VillageEnd + 1;

    public bool Crier => !Narrow && RoadWidth >= CrierRoad;

    public int CrierX => PalisadeX + 14;

    public int MageX => FrontX + (Compact ? 1 : 4);

    public int ArcherX => FrontX + (Compact ? 16 : 22);

    public int HeroX => FrontX + (Compact ? 31 : 40);

    public int MonsterX => FrontX + (Compact ? 53 : 67);

    public int BossX => FrontX + (Compact ? 51 : 65);

    public int FogX => FrontX + FrontWidth;

    public int SpawnX => FogX + Math.Min(22, FogWidth);

    public int SkillsX => FogX + FogWidth;

    public int SkillsWidth => Width - SkillsX;

    public int CartFrom(FrontierState state) => Narrow || Center(state, BuildingKind.Bakery) is not int center ? Margin : center - 9;

    public int CartTo => FrontX - 20;

    public int SlotX(int index) => VillageX + index * SlotWidth;

    public int SlotCenter(int index) => SlotX(index) + SlotWidth / 2;

    public int? Center(FrontierState state, BuildingKind kind) => !Narrow && state.PlotOf(kind) is int plot ? SlotCenter(plot) : null;

    public int FighterX(FighterKind kind) => kind switch
    {
        FighterKind.Archer => ArcherX,
        FighterKind.Mage => MageX,
        _ => HeroX,
    };

    public static FrontierLayout For(int width, int height)
    {
        int top = Math.Max(0, height - Rows);
        bool roomy = width >= 320;
        int skillSize = roomy ? 21 : 17;
        int skills = skillSize * 3 + 2 * 2 + 5;
        int front = roomy ? RoomyFront : CompactFront;
        int remain = width - Margin - skills - front;
        if (remain < NarrowSlot * Slots)
        {
            int fog = Math.Max(0, remain);
            return new FrontierLayout(width, top, Margin, 0, 0, Margin, fog, skillSize, !roomy);
        }

        int village = Math.Clamp(remain, NarrowSlot * Slots, ComfortSlot * Slots);
        int rest = remain - village;
        int road = Take(ref rest, ComfortRoad);
        int fogWidth = Take(ref rest, ComfortFog);
        village += Take(ref rest, WideSlot * Slots - village);
        road += Take(ref rest, WideRoad - road);
        fogWidth += Take(ref rest, WideFog - fogWidth);
        int slot = village / Slots;
        road += rest + village - slot * Slots;
        int frontX = Margin + slot * Slots + road;
        return new FrontierLayout(width, top, Margin, slot, road, frontX, fogWidth, skillSize, !roomy);
    }

    private static int Take(ref int rest, int wanted)
    {
        int taken = Math.Max(0, Math.Min(rest, wanted));
        rest -= taken;
        return taken;
    }
}
