using System;

namespace IdleBar.Cloud;

public sealed class ActionRefusedException : Exception
{
    public ActionRefusedException(string message)
        : base(message)
    {
    }
}
