using App.Domain.Game;

namespace App.Application.ReadModel.Projection;

public interface IServersProjection
{
    Task<IEnumerable<ServerDto>> GetServersAsync();
    Task<IEnumerable<ServerDto>> GetOnlineServersAsync();
    Task<ServerDto?> GetServerAsync(ServerModule.Id serverId);
}

public record ServerDto(Guid ServerId, string RegionLabel, string ServerLabel, bool IsOnline);