using System;
using System.IO;
using IdleBar.Cloud;

namespace IdleBar.Desktop;

public static class UpdateFailure
{
    public static bool Matches(Exception exception) =>
        TransportFailure.Matches(exception)
        || exception is OperationCanceledException or IOException or UnauthorizedAccessException or InvalidDataException or UpdateFailedException;
}
