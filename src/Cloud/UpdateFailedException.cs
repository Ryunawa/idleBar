using System;

namespace IdleBar.Cloud;

public sealed class UpdateFailedException : Exception
{
    public UpdateFailedException(string message)
        : base(message)
    {
    }
}
