using System;

namespace IdleBar.Cloud;

public sealed class CloudRequestException : Exception
{
    public CloudRequestException(string message)
        : base(message)
    {
    }
}
