using System;
using System.Threading.Tasks;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class FrontierSession
{
    private const string Reloaded = "Ta partie a été reprise telle qu'elle est sur le serveur.";
    private const string Away = "Ta partie est ouverte sur un autre PC. Clique sur l'or pour la reprendre ici.";

    private async Task<bool> ConnectAsync(bool gentle)
    {
        int generation = _generation;
        _busy = true;
        _clock.Attempted();
        _access.Recheck();
        if (Game is null && Status is SessionStatus.SignedOut or SessionStatus.Offline)
        {
            SetStatus(SessionStatus.Connecting);
        }

        try
        {
            string token = await _access.TokenAsync();
            FrontierReply reply = gentle ? await _api.RejoinAsync(token, _session) : await _api.ClaimAsync(token, _session);
            if (!Stale(generation))
            {
                Welcome(reply);
            }

            return !Stale(generation);
        }
        catch (Exception exception) when (Stale(generation) || Fails(exception))
        {
            if (!Stale(generation))
            {
                PlayOffline();
            }

            return false;
        }
        finally
        {
            if (!Stale(generation))
            {
                _busy = false;
            }
        }
    }

    private void Welcome(FrontierReply reply)
    {
        if (reply.Status == FrontierReply.Elsewhere)
        {
            Remember();
            SetStatus(SessionStatus.Elsewhere);
            Noticed?.Invoke(Away);
        }
        else if (reply.Frontier is FrontierData data)
        {
            Adopt(data, _keeper.UserId is string player ? _local.Read(player) : null);
        }
        else
        {
            SetStatus(SessionStatus.NeedsStart);
        }
    }

    private void Adopt(FrontierData data, LocalSave? local)
    {
        if (Game is not null && (data.Revision == _revision || FrontierLoader.SavedBy(data, _session, _revision)))
        {
            if (data.Hero is HeroLook hero)
            {
                Game.Adopt(hero, Roles.Parse(data.HeroRole) ?? Game.State.Role, data.RoleRetreats);
            }

            _revision = data.Revision;
            SavedRetreats = data.Retreats;
            _saveSoon = true;
            SetStatus(SessionStatus.Ready);
            return;
        }

        bool replacing = Game is not null;
        LoadPlan plan = FrontierLoader.Plan(data, local, DateTimeOffset.UtcNow);
        _revision = data.Revision;
        SavedRetreats = data.Retreats;
        _saveSoon = plan.Unsent;
        SetStatus(SessionStatus.Ready);
        Loaded?.Invoke(plan);
        if (replacing && !plan.Unsent)
        {
            Noticed?.Invoke(Reloaded);
        }
    }

    private void PlayOffline()
    {
        if (Game is not null || _keeper.UserId is not string player || _local.Read(player) is not LocalSave local)
        {
            return;
        }

        _revision = local.Revision;
        Loaded?.Invoke(FrontierLoader.Offline(local, DateTimeOffset.UtcNow));
    }

    private LocalSave Copy(string player, bool unsent, FrontierSave save) =>
        new(player, _revision, unsent, DateTimeOffset.UtcNow, save, _session);
}
