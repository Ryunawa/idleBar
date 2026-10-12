using System;
using System.Text.Json;

namespace IdleBar.Online;

public sealed record FrontierData(JsonElement Save, int Revision, DateTimeOffset SavedAt, double AwaySeconds, int VeteranSouls, string? VeteranName);
