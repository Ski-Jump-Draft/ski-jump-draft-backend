namespace App.Application.Matchmaking;

public record PremiumMatchmakingConfig(string Password);

public interface IPremiumMatchmakings
{
    Task<Guid> Add(string password, Guid matchmakingId);
    Task<bool> StartGameIfBelongsToMatchmaking(Guid matchmakingId, Guid gameId);
    Task<bool> EndGameIfRuns(Guid gameId);
    Task<bool> GameRunsByPremiumMatchmaking(string password);
    Task<bool> GameRunsByPremiumMatchmaking(Guid matchmakingId);
    Task Remove(Guid matchmakingId);
    Task<string?> GetPassword(Guid matchmakingId);
    Task<Guid?> GetPremiumMatchmakingId(string password);
    Task<bool> PremiumMatchmakingIsRunning(Guid matchmakingId);
    async Task<bool> PremiumMatchmakingIsRunning(string password) => await GetPremiumMatchmakingId(password) != null;
}

public interface IPremiumMatchmakingConfigurationStorage
{
    Task<ISet<PremiumMatchmakingConfig>> PremiumMatchmakingConfigs { get; }

    Task<PremiumMatchmakingConfig?> GetByPassword(string password);

    async Task<bool> PremiumMatchmakingPasswordIsValid(string password)
    {
        return await GetByPassword(password) is not null;
    }
}