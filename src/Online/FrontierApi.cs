using System;
using System.Threading.Tasks;
using IdleBar.Cloud;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed class FrontierApi
{
    private readonly SupabaseRpc _rpc;

    public FrontierApi(SupabaseRpc rpc)
    {
        _rpc = rpc;
    }

    public Task<RequirementsData> GetRequirementsAsync(string token) =>
        _rpc.CallAsync<NoArguments, RequirementsData>(token, "get_requirements", new NoArguments());

    public Task<FrontierReply> StartAsync(string token, Guid session) =>
        _rpc.CallAsync<SessionArguments, FrontierReply>(token, "start_frontier", new SessionArguments(session));

    public Task<FrontierReply> ClaimAsync(string token, Guid session) =>
        _rpc.CallAsync<SessionArguments, FrontierReply>(token, "claim_frontier", new SessionArguments(session));

    public Task<FrontierReply> RejoinAsync(string token, Guid session) =>
        _rpc.CallAsync<SessionArguments, FrontierReply>(token, "rejoin_frontier", new SessionArguments(session));

    public Task<FrontierReply> SaveAsync(string token, FrontierSave save, int revision, Guid session) =>
        _rpc.CallAsync<SaveArguments, FrontierReply>(token, "save_frontier", SaveArguments.From(save, revision, session));
}
