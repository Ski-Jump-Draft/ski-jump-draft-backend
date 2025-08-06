using App.Application.ReadModel.Projection;
using App.Domain.Game;

namespace App.Infrastructure.Projection.Hosting.Servers;

public class Test : IServersProjection
{
    public Task<IEnumerable<ServerDto>> GetServersAsync()
    {
        return Task.FromResult<IEnumerable<ServerDto>>([Server]);
    }

    public Task<IEnumerable<ServerDto>> GetOnlineServersAsync()
    {
        return GetServersAsync();
        // return Task.FromResult<IEnumerable<ServerDto>>([Server]);
    }

    public Task<ServerDto?> GetServerAsync(ServerModule.Id serverId)
    {
        if (serverId.Item == Server.ServerId)
        {
            return Task.FromResult(Server)!;
        }

        return Task.FromResult<ServerDto?>(null);
    }


    private ServerDto Server =>
        new(Guid.Parse("4f1467fd-75cd-40a4-a9a7-94c5522b7895"), "pl", "1", true);
}