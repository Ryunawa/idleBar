using System;

namespace IdleBar.Cloud;

public sealed class CloudAuthException : Exception
{
    public CloudAuthException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
