using System;

namespace IdleBar.Cloud;

public sealed class CloudRequestException : Exception
{
    public CloudRequestException(string message, int? status = null)
        : base(message)
    {
        Status = status;
    }

    public int? Status { get; }
}
