using System.Collections.Immutable;
using App.Domain._2.Matchmaking;

namespace App.Application._2.Messaging.Notifiers;

public interface IMatchmakingNotifier
{
    Task MatchmakingUpdated(MatchmakingUpdatedDto matchmaking);
}

public sealed record MatchmakingUpdatedDto(
    Guid MatchmakingId,
    string Status,
    IReadOnlyList<PlayerDto> Players
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
                .ToImmutableList()
        );
    }
}

public sealed record PlayerDto(
    Guid PlayerId,
    string Nick
);