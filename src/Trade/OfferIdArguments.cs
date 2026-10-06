using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record OfferIdArguments([property: JsonPropertyName("p_offer_id")] long OfferId);
