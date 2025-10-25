namespace App.Application.Game.PassPicksCount;

public interface IDraftPassPicksCountArchive
{
    Task<int> Get(Guid gameId, Guid playerId);
    Task<Dictionary<Guid, int>> GetDictionary(Guid gameId);
    Task Add(Guid gameId, Guid playerId);
}