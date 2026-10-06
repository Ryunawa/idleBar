using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdleBar.Cloud;

public static class TransportFailure
{
    public static bool Matches(Exception exception) =>
        exception is HttpRequestException or TaskCanceledException or CloudRequestException or JsonException;
}
