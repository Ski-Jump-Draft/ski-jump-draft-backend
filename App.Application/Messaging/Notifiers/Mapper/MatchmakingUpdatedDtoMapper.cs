using System.Collections.Immutable;
using App.Application.Bot;
using App.Domain.Matchmaking;
using Microsoft.FSharp.Core;

namespace App.Application.Messaging.Notifiers.Mapper;

public class MatchmakingUpdatedDtoMapper(
    IBotRegistry botRegistry // jedyna zależność teraz, ale łatwo rozbudować
)
{
    public MatchmakingUpdatedDto FromDomain(Domain.Matchmaking.Matchmaking matchmaking)
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
                .Select(player => CreatePlayerDto(player.Id.Item,
                    PlayerModule.NickModule.value(player.Nick),
                    matchmaking))
                .ToImmutableList(),
            matchmaking.PlayersCount,
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers),
            SettingsModule.MinPlayersModule.value(matchmaking.MinPlayersCount),
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount)
        );
    }

    public PlayerJoinedDto PlayerJoinedFromDomain(Guid playerId, string playerNick,
        Domain.Matchmaking.Matchmaking matchmaking)
        => new(
            matchmaking.Id_.Item,
            CreatePlayerDto(playerId, playerNick, matchmaking),
            matchmaking.PlayersCount,
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount),
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers)
        );

    public PlayerLeftDto PlayerLeftFromDomain(Guid playerId, string playerNick,
        Domain.Matchmaking.Matchmaking matchmaking)
        => new(
            matchmaking.Id_.Item,
            CreatePlayerDto(playerId, playerNick, matchmaking),
            matchmaking.PlayersCount,
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount),
            OptionModule.ToNullable(matchmaking.MinRequiredPlayers)
        );

    private MatchmakingPlayerDto CreatePlayerDto(Guid playerId, string playerNick,
        Domain.Matchmaking.Matchmaking matchmaking)
    {
        var isBot = botRegistry.IsMatchmakingBot(matchmaking.Id_.Item, playerId);
        return new MatchmakingPlayerDto(playerId, playerNick, isBot);
    }
}