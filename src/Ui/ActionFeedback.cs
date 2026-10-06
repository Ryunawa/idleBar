using System;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;

namespace IdleBar.Ui;

public static class ActionFeedback
{
    private const string Unreachable = "Le serveur est injoignable. Réessaie dans un instant.";

    public static async Task<string?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (ActionRefusedException exception)
        {
            return exception.Message;
        }
        catch (CloudAuthException exception)
        {
            return exception.Message;
        }
        catch (Exception exception) when (TransportFailure.Matches(exception))
        {
            GD.PushWarning($"Action impossible : {exception.Message}");
            return Unreachable;
        }
    }
}
