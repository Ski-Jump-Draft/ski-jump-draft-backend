using App.Application.Messaging.Notifiers;

namespace App.Application.Matchmaking;

public interface IMatchmakingUpdatedDtoStorage
{
    Task<MatchmakingUpdatedDto?> Get(Guid matchmakingId);
    Task Set(Guid matchmakingId, MatchmakingUpdatedDto dto);
}