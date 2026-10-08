using System;

namespace IdleBar.Online;

public sealed class OutdatedClientException : Exception
{
    public OutdatedClientException(string minimum)
        : base($"Cette version d'IdleBar est trop ancienne : il faut au moins la version {minimum}.")
    {
    }
}
