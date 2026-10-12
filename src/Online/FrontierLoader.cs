using System;
using System.Text.Json;
using IdleBar.Frontier;

namespace IdleBar.Online;

public static class FrontierLoader
{
    public static LoadPlan Plan(FrontierData data, LocalSave? local, DateTimeOffset now)
    {
        if (local is { Unsent: true } && local.Revision == data.Revision)
        {
            FrontierState offline = SaveCodec.Restore(local.Save);
            offline.VeteranName = data.VeteranName;
            return new LoadPlan(offline, Since(local, now), true);
        }

        FrontierSave? save = data.Save.ValueKind == JsonValueKind.Object ? SaveCodec.Deserialize(data.Save.GetRawText()) : null;
        FrontierState state = save is null ? new FrontierState { Souls = data.VeteranSouls } : SaveCodec.Restore(save);
        state.VeteranName = data.VeteranName;
        return new LoadPlan(state, save is null ? 0 : Math.Max(0, data.AwaySeconds), false);
    }

    public static LoadPlan Offline(LocalSave local, DateTimeOffset now) => new(SaveCodec.Restore(local.Save), Since(local, now), true);

    private static double Since(LocalSave local, DateTimeOffset now) => Math.Max(0, (now - local.WrittenAt).TotalSeconds);
}
