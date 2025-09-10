using System.Collections.Immutable;
using App.Domain.Matchmaking;
using Microsoft.FSharp.Core;

namespace App.Application.Messaging.Notifiers;

public interface IMatchmakingNotifier
{
    Task MatchmakingUpdated(MatchmakingUpdatedDto matchmaking);
    Task PlayerJoined(PlayerJoinedDto playerJoined);
    Task PlayerLeft(PlayerLeftDto playerLeft);
}

public sealed record PlayerJoinedDto(
    Guid MatchmakingId,
    PlayerDto Player,
    int PlayersCount,
    int MaxPlayers,
    int? MinRequiredPlayers
);

public sealed record PlayerLeftDto(
    Guid MatchmakingId,
    PlayerDto Player,
    int PlayersCount,
    int MaxPlayers,
    int? MinRequiredPlayers
);

public sealed record MatchmakingUpdatedDto(
    Guid MatchmakingId,
    string Status,
    IReadOnlyList<PlayerDto> Players,
    int PlayersCount,
    int? MinRequiredPlayers,
    int MinPlayers,
    int MaxPlayers
);

public static class MatchmakingNotifierMappers
{
    public static MatchmakingUpdatedDto MatchmakingUpdatedFromDomain(App.Domain.Matchmaking.Matchmaking matchmaking)
    {
        var statusString = matchmaking.Status_ switch
        {
            Status.Ended endedStatus => endedStatus.Result switch
            {
                { IsSucceeded: true } => "Ended Succeeded",
                { IsNotEnoughPlayers: true } => "Ended NotEnoughPlayers",
                _ => throw new ArgumentOutOfRangeException()
            },
            Status.Failed failedStatus => "Failed " + failedStatus,
            var s when s.IsRunning => "Running",
            _ => throw new ArgumentOutOfRangeException()
        };

        return new MatchmakingUpdatedDto(
            matchmaking.Id_.Item,
            statusString,
            matchmaking.Players_
                .Select(player => new PlayerDto(player.Id.Item, PlayerModule.NickModule.value(player.Nick)))
                .ToImmutableList(),
            matchmaking.PlayersCount,
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers),
            SettingsModule.MinPlayersModule.value(matchmaking.MinPlayersCount),
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount)
        );
    }

    public static PlayerJoinedDto PlayerJoinedFromDomain(Guid playerId, string playerNick,
        App.Domain.Matchmaking.Matchmaking matchmaking)
    {
        return new PlayerJoinedDto(
            matchmaking.Id_.Item,
            new PlayerDto(playerId, playerNick),
            matchmaking.PlayersCount,
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount),
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers)
        );
    }
    
    public static PlayerLeftDto PlayerLeftFromDomain(Guid playerId, string playerNick,
        App.Domain.Matchmaking.Matchmaking matchmaking)
    {
        return new PlayerLeftDto(
            matchmaking.Id_.Item,
            new PlayerDto(playerId, playerNick),
            matchmaking.PlayersCount,
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount),
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers)
        );
    }
}

public sealed record MatchmakingPlayerDto(
    Guid PlayerId,
    string Nick
);