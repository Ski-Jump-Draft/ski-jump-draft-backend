using System.Collections.Immutable;
using App.Domain._2.Matchmaking;
using Microsoft.FSharp.Core;

namespace App.Application._2.Messaging.Notifiers;

public interface IMatchmakingNotifier
{
    Task MatchmakingUpdated(MatchmakingUpdatedDto matchmaking);
}

public sealed record MatchmakingUpdatedDto(
    Guid MatchmakingId,
    string Status,
    IReadOnlyList<PlayerDto> Players,
    int PlayersCount,
    int? MinRequiredPlayers,
    int MinPlayers,
    int MaxPlayers
);

public static class MatchmakingDtoMapper
{
    public static MatchmakingUpdatedDto FromDomain(App.Domain._2.Matchmaking.Matchmaking matchmaking)
    {
        return new MatchmakingUpdatedDto(
            matchmaking.Id_.Item,
            matchmaking.Status_.ToString(),
            matchmaking.Players_
                .Select(player => new PlayerDto(player.Id.Item, PlayerModule.NickModule.value(player.Nick)))
                .ToImmutableList(),
            matchmaking.PlayersCount,
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers),
            SettingsModule.MinPlayersModule.value(matchmaking.MinPlayersCount),
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount)
        );
    }
}

public sealed record PlayerDto(
    Guid PlayerId,
    string Nick
);