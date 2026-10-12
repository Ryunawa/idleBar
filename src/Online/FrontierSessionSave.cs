using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class FrontierSession
{
    private const string Reloaded = "Ta partie a été reprise telle qu'elle est sur le serveur.";
    private const string Away = "Ta partie est ouverte sur un autre PC. Clique sur l'or pour la reprendre ici.";

    public Task FlushAsync()
    {
        Remember();
        return Game is not null && !_busy && Status is SessionStatus.Ready or SessionStatus.Offline ? SaveAsync() : Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (_keeper.UserId is not string player || Snapshot() is not FrontierSave save)
        {
            return;
        }

        int generation = _generation;
        _busy = true;
        _saveSoon = false;
        _clock.Saved();
        _local.Write(new LocalSave(player, _revision, true, DateTimeOffset.UtcNow, save));
        try
        {
            string token = await _access.TokenAsync();
            FrontierReply reply = await _api.SaveAsync(token, save, _revision, _session);
            if (!Stale(generation))
            {
                Settle(reply, player, save);
            }
        }
        catch (Exception exception) when (Stale(generation) || Fails(exception))
        {
            _saveSoon = !Stale(generation);
        }
        finally
        {
            if (!Stale(generation))
            {
                _busy = false;
            }
        }
    }

    private void Settle(FrontierReply reply, string player, FrontierSave save)
    {
        switch (reply.Status)
        {
            case FrontierReply.Ok when reply.Frontier is FrontierData data:
                _revision = data.Revision;
                _local.Write(new LocalSave(player, _revision, false, DateTimeOffset.UtcNow, save));
                SetStatus(SessionStatus.Ready);
                break;
            case FrontierReply.Elsewhere:
                SetStatus(SessionStatus.Elsewhere);
                Noticed?.Invoke(Away);
                break;
            default:
                if (reply.Frontier is FrontierData server)
                {
                    _revision = -1;
                    _local.Forget();
                    Game = null;
                    Adopt(server, null);
                    Noticed?.Invoke(Reloaded);
                }

                break;
        }
    }

    private void Remember()
    {
        if (Status is (SessionStatus.Ready or SessionStatus.Offline) && _keeper.UserId is string player && Snapshot() is FrontierSave save)
        {
            _local.Write(new LocalSave(player, _revision, true, DateTimeOffset.UtcNow, save));
        }
    }

    private FrontierSave? Snapshot()
    {
        if (Game is not FrontierGame game)
        {
            return null;
        }

        FrontierSave save = SaveCodec.Capture(game.State);
        if (SaveCodec.Valid(save))
        {
            return save;
        }

        GD.PushWarning("Sauvegarde ignorée : un nombre n'est pas fini.");
        return null;
    }

    private bool Fails(Exception exception)
    {
        switch (exception)
        {
            case CloudAuthException:
                GD.PushWarning($"Session refusée : {exception.Message}");
                Reset();
                _keeper.SignOut();
                return true;
            case OutdatedClientException:
                SetStatus(SessionStatus.UpdateRequired);
                return true;
            case ActionRefusedException:
            case not null when TransportFailure.Matches(exception):
                GD.PushWarning($"Serveur injoignable : {exception.Message}");
                SetStatus(SessionStatus.Offline);
                return true;
            default:
                return false;
        }
    }

    private void SetStatus(SessionStatus status)
    {
        Status = status;
        Changed?.Invoke();
    }
}
