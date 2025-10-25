namespace App.Web.Security;

public interface IGamePlayerMappingStore
{
    void Store(Guid matchmakingId, Guid gameId, IReadOnlyDictionary<Guid, Guid> matchmakingToGame);
    bool TryGetByGame(Guid gameId, out Guid matchmakingId, out IReadOnlyDictionary<Guid, Guid> matchmakingToGame);
    void Remove(Guid gameId);
}