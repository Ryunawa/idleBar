using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record ContractIdArguments([property: JsonPropertyName("p_contract_id")] long ContractId);
