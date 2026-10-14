using System;
using IdleBar.Frontier;

namespace IdleBar.Online;

public static class FrontierLoader
{
    public static LoadPlan Plan(FrontierData data, LocalSave? local, DateTimeOffset now) =>
        local is { Unsent: true, Save.Version: >= SaveCodec.LevelCurveVersion } && (local.Revision == data.Revision || SavedLater(data, local))
            ? new LoadPlan(SaveCodec.Restore(data.WithHero(local.Save)), Since(local, now), true)
            : new LoadPlan(SaveCodec.Restore(data.ToSave()), Math.Max(0, data.AwaySeconds), false);

    public static LoadPlan Offline(LocalSave local, DateTimeOffset now) => new(SaveCodec.Restore(local.Save), Since(local, now), true);

    public static bool SavedBy(FrontierData data, Guid session, int revision) =>
        session != Guid.Empty && data.SavedBy == session && data.Revision > revision;

    private static bool SavedLater(FrontierData data, LocalSave local) => SavedBy(data, local.Session, local.Revision);

    private static double Since(LocalSave local, DateTimeOffset now) => Math.Max(0, (now - local.WrittenAt).TotalSeconds);
}
