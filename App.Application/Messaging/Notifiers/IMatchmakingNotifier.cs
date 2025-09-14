namespace App.Application.Messaging.Notifiers;

public interface IMatchmakingNotifier
{
    Task MatchmakingUpdated(MatchmakingUpdatedDto matchmaking);
    Task PlayerJoined(PlayerJoinedDto playerJoined);
    Task PlayerLeft(PlayerLeftDto playerLeft);
}

public sealed record MatchmakingPlayerDto(Guid PlayerId, string Nick, bool IsBot);

public sealed record PlayerJoinedDto(
    Guid MatchmakingId,
    MatchmakingPlayerDto Player,
    int PlayersCount,
    int MaxPlayers,
    int? MinRequiredPlayers
);

public sealed record PlayerLeftDto(
    Guid MatchmakingId,
    MatchmakingPlayerDto Player,
    int PlayersCount,
    int MaxPlayers,
    int? MinRequiredPlayers
);

public sealed record MatchmakingUpdatedDto(
    Guid MatchmakingId,
    string Status,
    IReadOnlyList<MatchmakingPlayerDto> Players,
    int PlayersCount,
    int? MinRequiredPlayers,
    int MinPlayers,
    int MaxPlayers
);