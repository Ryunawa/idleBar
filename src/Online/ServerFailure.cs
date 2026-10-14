using System;
using IdleBar.Cloud;

namespace IdleBar.Online;

public static class ServerFailure
{
    public static string? Reason(Exception exception) => exception switch
    {
        ActionRefusedException or OutdatedClientException => exception.Message,
        CloudRequestException { Status: >= 400 and < 500 } => $"Le serveur a refusé l'action : {exception.Message}",
        _ => null,
    };
}
