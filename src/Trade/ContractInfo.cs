using System;

namespace IdleBar.Trade;

public sealed record ContractInfo(
    long Id,
    string OriginTownId,
    string DestinationTownId,
    string GoodId,
    int Quantity,
    int Reward,
    int GameFee,
    int Deposit,
    ContractStatus Status,
    DateTimeOffset TakeoverAt,
    DateTimeOffset GameArrivesAt,
    DateTimeOffset? CarriedAt,
    DateTimeOffset? Deadline,
    DateTimeOffset? ClosedAt,
    string? Shipper,
    string? Carrier,
    ContractRole? Role,
    bool Seen)
{
    public bool IsUnderway => Status is ContractStatus.Open or ContractStatus.Carried or ContractStatus.Shipped;

    public DateTimeOffset? NextChangeAt => Status switch
    {
        ContractStatus.Open => TakeoverAt,
        ContractStatus.Shipped => GameArrivesAt,
        ContractStatus.Carried => Deadline,
        _ => null,
    };
}
