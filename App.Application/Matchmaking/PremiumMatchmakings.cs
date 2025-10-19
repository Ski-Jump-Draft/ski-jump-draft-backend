namespace App.Application.Matchmaking;

public record PremiumMatchmakingConfig(string Password);

public interface IPremiumMatchmakingGames
{
    Task<Guid> Add(string password, Guid matchmakingId);
    Task EndMatchmaking(Guid matchmakingId);
    Task<bool> StartGameIfBelongsToMatchmaking(Guid matchmakingId, Guid gameId);
    Task<int> GetGamesCount();
    Task<bool> EndGameIfRuns(Guid gameId);
    Task<bool> GameRunsByPremiumMatchmaking(string password);
    Task<string?> GetPassword(Guid matchmakingId);
    Task<Guid?> GetPremiumMatchmakingId(string password);
}

public interface IPremiumMatchmakingConfigurationStorage
{
    Task<HashSet<PremiumMatchmakingConfig>> PremiumMatchmakingConfigs { get; }

    Task<PremiumMatchmakingConfig?> GetByPassword(string password);

    async Task<bool> PremiumMatchmakingPasswordIsValid(string password)
    {
        return await GetByPassword(password) is not null;
    }
}