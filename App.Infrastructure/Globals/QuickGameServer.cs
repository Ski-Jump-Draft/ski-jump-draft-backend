using App.Application.Abstractions;
using App.Application.ReadModel.Projection;
using App.Domain.Game;

namespace App.Infrastructure.Globals;

public class OnlyQuickGameServerProvider(IServersProjection servers) : IQuickGameServerProvider
{
    public async Task<ServerModule.Id> Provide()
    {
        var onlineServers = (await servers.GetOnlineServersAsync()).ToArray();
        return onlineServers.Length switch
        {
            > 1 => throw new NotSupportedException("We don't support more than one server for now."),
            1 => ServerModule.Id.NewId(onlineServers.Single().ServerId),
            _ => throw new NotSupportedException("No server available")
        };
    }
}