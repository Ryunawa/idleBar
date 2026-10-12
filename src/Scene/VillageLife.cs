using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public sealed class VillageLife
{
    private const int MaxVillagers = 9;
    private const float SmokeSeconds = 0.45f;

    private static readonly Rgba[] Shirts = [Palette.Blue, Palette.Red, Palette.Leaf, Palette.Plum, Palette.Orange, Palette.NavyBlue, Palette.Brick, Palette.Pink];
    private static readonly Rgba[] Hairs = [Palette.Soil, Palette.Bark, Palette.Gold, Palette.Ink, Palette.Silver];

    private float _smoke;

    public List<Villager> Villagers { get; } = [];

    public Crier Crier { get; } = new();

    public void Update(float dt, FrontierState state, FrontierLayout layout, SceneFx fx, Random random, bool night)
    {
        Crier.Update(dt, random);
        int wanted = layout.Narrow ? 0 : Math.Min(MaxVillagers, 2 + state.Buildings.Count(level => level > 0));
        while (Villagers.Count < wanted)
        {
            Villagers.Add(new Villager(
                layout.VillageX + (float)random.NextDouble() * (layout.VillageEnd - layout.VillageX - 4),
                random.Next(2) == 0 ? -1 : 1,
                4 + (float)random.NextDouble() * 5,
                Shirts[random.Next(Shirts.Length)],
                Hairs[random.Next(Hairs.Length)],
                (float)random.NextDouble() * 10));
        }

        if (Villagers.Count > wanted)
        {
            Villagers.RemoveRange(wanted, Villagers.Count - wanted);
        }

        foreach (Villager villager in Villagers)
        {
            Walk(villager, dt, layout, random);
        }

        Smoke(dt, state, layout, fx, random, night);
    }

    private static void Walk(Villager villager, float dt, FrontierLayout layout, Random random)
    {
        if (villager.Pause > 0)
        {
            villager.Pause -= dt;
            return;
        }

        villager.X += villager.Direction * villager.Speed * dt;
        float right = layout.VillageEnd - 4;
        if (villager.X < layout.VillageX + 2 || villager.X > right)
        {
            villager.X = Math.Clamp(villager.X, layout.VillageX + 2, right);
            villager.Direction = -villager.Direction;
        }

        if (random.NextDouble() < dt * 0.25)
        {
            villager.Pause = 0.8f + (float)random.NextDouble() * 2.2f;
        }
        else if (random.NextDouble() < dt * 0.08)
        {
            villager.Direction = -villager.Direction;
        }
    }

    private void Smoke(float dt, FrontierState state, FrontierLayout layout, SceneFx fx, Random random, bool night)
    {
        if (layout.Narrow)
        {
            return;
        }

        _smoke -= dt;
        if (_smoke <= 0)
        {
            _smoke = SmokeSeconds;
            Rgba color = night ? Palette.Steel : Palette.Silver;
            if (state.Level(BuildingKind.Bakery) > 0)
            {
                fx.Add(new Particle(ParticleKind.Smoke, layout.SlotCenter(BuildingKind.Bakery) + 6, 3, 2 + (float)random.NextDouble() * 2, -2.5f, 0, 2.2f, color));
            }

            if (state.Level(BuildingKind.Forge) > 0)
            {
                fx.Add(new Particle(ParticleKind.Smoke, layout.SlotCenter(BuildingKind.Forge) + 5, 1, 2 + (float)random.NextDouble() * 2, -3f, 0, 2.2f, color));
            }
        }

        if (state.Level(BuildingKind.Forge) > 0 && random.NextDouble() < dt * 2.5)
        {
            Rgba spark = random.Next(2) == 0 ? Palette.Yellow : Palette.Orange;
            fx.Add(new Particle(ParticleKind.Dot, layout.SlotCenter(BuildingKind.Forge) + 14, 19, ((float)random.NextDouble() - 0.5f) * 24, -18 - (float)random.NextDouble() * 12, 70, 0.35f, spark));
        }
    }
}
