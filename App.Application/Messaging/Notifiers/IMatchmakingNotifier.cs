namespace App.Application.Messaging.Notifiers;

public interface IMatchmakingNotifier
{
    Task MatchmakingUpdated(MatchmakingUpdatedDto matchmaking);
    Task PlayerJoined(PlayerJoinedDto playerJoined);
    Task PlayerLeft(PlayerLeftDto playerLeft);
}

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

public sealed record MatchmakingPlayerDto(Guid PlayerId, string Nick, bool IsBot, DateTimeOffset JoinedAt);

public sealed record MatchmakingUpdatedDto(
    Guid MatchmakingId,
    bool IsPremium,
    string Status,
    IReadOnlyList<MatchmakingPlayerDto> Players,
    int PlayersCount,
    int? RequiredPlayersToMin,
    int MinPlayers,
    int MaxPlayers,
    DateTimeOffset StartedAt,
    DateTimeOffset ForceEndAt, // When should end regardless of circumstances
    DateTimeOffset? EndedAt,
    bool EndAfterNoUpdate,
    DateTimeOffset? ShouldEndAcceleratedAt // When min/max reached
);