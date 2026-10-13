using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public sealed class SceneMotion
{
    public const float SwingSeconds = 0.16f;
    public const float BuildSeconds = 1f;
    public const float RevealSeconds = 1.6f;
    public const float RecruitSeconds = 1.5f;
    public const float CorpseSeconds = 0.22f;
    public const float FleeSeconds = 1.3f;

    public float HeroSwing { get; set; }

    public float MonsterFlash { get; set; }

    public float MonsterKick { get; set; }

    public float MonsterLunge { get; set; }

    public float[] Hurt { get; } = new float[FrontierState.FighterCount];

    public Dictionary<long, float> AllyHurt { get; } = [];

    public Dictionary<long, float> AllySwing { get; } = [];

    public int MonsterShift => MonsterLunge > 0 ? -3 : MonsterKick > 0 ? 1 : 0;

    public float MageGlow { get; set; }

    public float Lightning { get; set; }

    public float Shake { get; set; }

    public float Flash { get; set; }

    public float Rain { get; set; }

    public float Replay { get; set; }

    public string ReplayLabel { get; set; } = string.Empty;

    public List<ArtPoint> BoltPath { get; } = [];

    public MonsterBox? Corpse { get; set; }

    public float CorpseTime { get; set; }

    public MonsterBox? Fleeing { get; set; }

    public float FleeTime { get; set; }

    public float?[] Build { get; } = new float?[FrontierState.PlotCount];

    public float?[] Reveal { get; } = new float?[FrontierState.PlotCount];

    public float?[] Recruit { get; } = new float?[FrontierState.FighterCount];

    public void Update(float dt)
    {
        HeroSwing = Math.Max(0, HeroSwing - dt / SwingSeconds);
        MonsterFlash = Math.Max(0, MonsterFlash - dt);
        MonsterKick = Math.Max(0, MonsterKick - dt);
        MonsterLunge = Math.Max(0, MonsterLunge - dt);
        for (int index = 0; index < Hurt.Length; index++)
        {
            Hurt[index] = Math.Max(0, Hurt[index] - dt);
        }

        Decay(AllyHurt, dt);
        Decay(AllySwing, dt / SwingSeconds);

        MageGlow = Math.Max(0, MageGlow - dt);
        Lightning = Math.Max(0, Lightning - dt);
        Shake = Math.Max(0, Shake - dt);
        Flash = Math.Max(0, Flash - dt);
        Rain = Math.Max(0, Rain - dt);
        Replay = Math.Max(0, Replay - dt);
        Advance(Build, dt, BuildSeconds);
        Advance(Reveal, dt, RevealSeconds);
        Advance(Recruit, dt, RecruitSeconds);
        if (Corpse is not null && (CorpseTime += dt) > CorpseSeconds)
        {
            Corpse = null;
        }

        if (Fleeing is not null && (FleeTime += dt) > FleeSeconds)
        {
            Fleeing = null;
        }
    }

    public void Forget()
    {
        Array.Clear(Build);
        Array.Clear(Reveal);
        Array.Clear(Recruit);
        Corpse = null;
        Fleeing = null;
    }

    private static void Decay(Dictionary<long, float> timers, float amount)
    {
        foreach (long key in timers.Keys.ToList())
        {
            float left = timers[key] - amount;
            if (left > 0)
            {
                timers[key] = left;
            }
            else
            {
                timers.Remove(key);
            }
        }
    }

    private static void Advance(float?[] timers, float dt, float limit)
    {
        for (int index = 0; index < timers.Length; index++)
        {
            if (timers[index] is float time)
            {
                timers[index] = time + dt >= limit ? null : time + dt;
            }
        }
    }
}
