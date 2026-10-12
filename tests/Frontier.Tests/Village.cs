using System;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public static class Village
{
    public static void Build(FrontierState state, BuildingKind kind, int level, int plot = -1)
    {
        int index = plot >= 0 ? plot : state.PlotOf(kind) ?? Array.IndexOf(state.Built, null);
        state.Built[index] = kind;
        state.Levels[index] = level;
    }
}
