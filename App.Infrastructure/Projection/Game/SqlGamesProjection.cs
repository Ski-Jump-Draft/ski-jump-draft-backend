using App.Application.Projection;

namespace App.Infrastructure.Projection.Game;

public class SqlGamesProjection : IGamesProjection
{
    // TODO
    public Task<IEnumerable<GameDto>> GetActiveGamesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<GameMatchmakingDto?> GetGameMatchmakingInfo(System.Guid gameId)
    {
        throw new NotImplementedException();
    }
}