using System.Collections.Immutable;
using App.Application.Bot;
using App.Application.Extensions;
using App.Domain.Matchmaking;
using Microsoft.FSharp.Core;

namespace App.Application.Messaging.Notifiers.Mapper;

public class MatchmakingUpdatedDtoMapper(
    IBotRegistry botRegistry
)
{
    public MatchmakingUpdatedDto FromDomain(Domain.Matchmaking.Matchmaking matchmaking, IBotRegistry botRegistry,
        DateTimeOffset now)
    {
        var statusString = matchmaking.Status_.FormattedStatus();

        return new MatchmakingUpdatedDto(
            matchmaking.Id_.Item,
            matchmaking.IsPremium_,
            statusString,
            matchmaking.Players_
                .Select(player => CreatePlayerDto(player,
                    botRegistry.IsMatchmakingBot(matchmaking.Id_.Item, player.Id.Item),
                    matchmaking))
                .ToImmutableList(),
            matchmaking.PlayersCount,
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers),
            SettingsModule.MinPlayersModule.value(matchmaking.MinPlayersCount),
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount),
            matchmaking.StartedAt_,
            matchmaking.ForceEndAt(now),
            matchmaking.EndedAt_.ToNullable(),
            matchmaking.EndPolicy.IsAfterNoUpdate,
            matchmaking.AcceleratedEndAt(now).ToNullable()
        );
    }

    public PlayerJoinedDto PlayerJoinedFromDomain(Domain.Matchmaking.Player player, bool isBot,
        Domain.Matchmaking.Matchmaking matchmaking)
        => new(
            matchmaking.Id_.Item,
            CreatePlayerDto(player, isBot, matchmaking),
            matchmaking.PlayersCount,
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount),
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers)
        );

    public PlayerLeftDto PlayerLeftFromDomain(Domain.Matchmaking.Player player, bool isBot,
        Domain.Matchmaking.Matchmaking matchmaking)
        => new(
            matchmaking.Id_.Item,
            CreatePlayerDto(player, isBot, matchmaking),
            matchmaking.PlayersCount,
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount),
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers)
        );

    private MatchmakingPlayerDto CreatePlayerDto(Domain.Matchmaking.Player player, bool isBot,
        Domain.Matchmaking.Matchmaking matchmaking)
    {
        return new MatchmakingPlayerDto(player.Id.Item, PlayerModule.NickModule.value(player.Nick), isBot,
            player.JoinedAt);
    }
}