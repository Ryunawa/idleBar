using System;
using IdleBar.Frontier;

namespace IdleBar.Online;

public static class FrontierLoader
{
    public static LoadPlan Plan(FrontierData data, LocalSave? local, DateTimeOffset now) =>
        local is { Unsent: true } && local.Revision == data.Revision
            ? new LoadPlan(SaveCodec.Restore(local.Save), Since(local, now), true)
            : new LoadPlan(SaveCodec.Restore(data.ToSave()), Math.Max(0, data.AwaySeconds), false);

    public static LoadPlan Offline(LocalSave local, DateTimeOffset now) => new(SaveCodec.Restore(local.Save), Since(local, now), true);

    private static double Since(LocalSave local, DateTimeOffset now) => Math.Max(0, (now - local.WrittenAt).TotalSeconds);
}
