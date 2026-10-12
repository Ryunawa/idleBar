using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class FrontierSession
{
    private const string Reloaded = "Ta partie a été reprise telle qu'elle est sur le serveur.";
    private const string Away = "Ta partie est ouverte sur un autre PC. Clique sur l'or pour la reprendre ici.";

    public Task FlushAsync() =>
        Game is not null && !_busy && Status is SessionStatus.Ready or SessionStatus.Offline ? SaveAsync() : Task.CompletedTask;

    private async Task SaveAsync()
    {
        if (Game is not FrontierGame game || _keeper.UserId is not string player)
        {
            return;
        }

        FrontierSave save = SaveCodec.Capture(game.State);
        if (!SaveCodec.Valid(save))
        {
            GD.PushWarning("Sauvegarde ignorée : un nombre n'est pas fini.");
            return;
        }

        _busy = true;
        _saveSoon = false;
        _clock.Saved();
        _local.Write(new LocalSave(player, _revision, true, DateTimeOffset.UtcNow, save));
        try
        {
            string token = await _access.TokenAsync();
            FrontierReply reply = await _api.SaveAsync(token, save, _revision, _session);
            Settle(reply, player, save);
        }
        catch (Exception exception) when (Fails(exception))
        {
            _saveSoon = true;
        }
        finally
        {
            _busy = false;
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
}
