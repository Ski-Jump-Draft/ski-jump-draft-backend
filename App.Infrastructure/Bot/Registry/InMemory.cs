using App.Application.Bot;

namespace App.Infrastructure.Bot.Registry;

public class InMemory : IBotRegistry
{
    private readonly Dictionary<Guid, HashSet<Guid>> _matchmakingBots = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _gameBots = new();

    public void RegisterMatchmakingBot(Guid matchmakingId, Guid playerId)
    {
        if (!_matchmakingBots.TryGetValue(matchmakingId, out var set))
        {
            set = new HashSet<Guid>();
            _matchmakingBots[matchmakingId] = set;
        }

        set.Add(playerId);
    }

    public void RegisterGameBot(Guid gameId, Guid playerId)
    {
        if (!_gameBots.TryGetValue(gameId, out var set))
        {
            set = new HashSet<Guid>();
            _gameBots[gameId] = set;
        }

        set.Add(playerId);
    }

    public IReadOnlyList<MatchmakingBotDto> MatchmakingBots(Guid matchmakingId) =>
        _matchmakingBots.TryGetValue(matchmakingId, out var set)
            ? set.Select(playerId => new MatchmakingBotDto(playerId)).ToList()
            : Array.Empty<MatchmakingBotDto>();

    public IReadOnlyList<GameBotDto> GameBots(Guid gameId) =>
        _gameBots.TryGetValue(gameId, out var set)
            ? set.Select(playerId => new GameBotDto(playerId)).ToList()
            : Array.Empty<GameBotDto>();

    public bool IsMatchmakingBot(Guid matchmakingId, Guid playerId) =>
        _matchmakingBots.TryGetValue(matchmakingId, out var set) && set.Contains(playerId);

    public bool IsGameBot(Guid gameId, Guid playerId) =>
        _gameBots.TryGetValue(gameId, out var set) && set.Contains(playerId);
}