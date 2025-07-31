using System.Collections.Immutable;
using App.Application.Commanding;
using App.Application.Commanding.Mappers;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Projection;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Game.Exception;
using App.Application.UseCase.Helper;
using App.Domain.Game;
using App.Domain.Game.Hosting;
using App.Domain.Shared;
using App.Domain.Repositories;
using Microsoft.FSharp.Collections;
using Settings = App.Domain.Game.Settings;

namespace App.Application.UseCase.Game.QuickGame.Create;

public record Command(
    Guid MatchmakingId
) : ICommand<App.Domain.Game.Game>;

public class Handler(
    IGameRepository games,
    //IMatchmakingParticipantRepository matchmakingParticipantRepository,
    IGameParticipantRepository gameParticipantRepository,
    IMatchmakingRepository matchmakings,
    IMatchmakingParticipantsProjection matchmakingParticipantsProjection,
    IGameParticipantFactory gameParticipantFactory,
    IQuickGameServerProvider quickGameServerProvider,
    IQuickGameSettingsProvider quickGameSettingsProvider,
    IGuid guid
) : ICommandHandler<Command, App.Domain.Game.Game>
{
    public async Task<App.Domain.Game.Game> HandleAsync(Command command, CancellationToken ct)
    {
        var matchmakingId = Domain.Matchmaking.Id.NewId(command.MatchmakingId);
        var matchmaking = await matchmakings.LoadAsync(matchmakingId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(matchmakingId.Item));

        var matchmakingParticipantDtos =
            (await matchmakingParticipantsProjection.GetParticipantsByMatchmakingIdAsync(matchmakingId)).ToArray();
        var gameParticipants =
            matchmakingParticipantDtos.Select(gameParticipantFactory.CreateFromDto).ToImmutableArray();
        var gameParticipantIds = gameParticipants.Select(participant => participant.Id);
        
        foreach (var gameParticipant in gameParticipants)
        {
            await gameParticipantRepository.SaveAsync(gameParticipant.Id, gameParticipant);
        }

        var serverId = await quickGameServerProvider.Provide();

        var gameId = Id.Id.NewId(guid.NewGuid());
        var gameVersion = AggregateVersion.AggregateVersion.NewAggregateVersion(0u);
        var settings = await quickGameSettingsProvider.Provide();

        var game = Domain.Game.Game.Create(gameId, gameVersion, ListModule.OfSeq(gameParticipantIds), serverId,
            settings);

        if (game.IsOk)
        {
            var (gameAggregate, events) = game.ResultValue;
            var correlationId = guid.NewGuid();
            var causationId = correlationId;
            var expectedVersion = gameAggregate.Version_;
            await games.SaveAsync(gameAggregate, events, expectedVersion, correlationId, causationId, ct)
                .AwaitOrWrap(_ => new CreatingQuickGameFailedException(matchmaking));
            await Task.WhenAll(
                gameParticipants
                    .Select(gp =>
                        gameParticipantRepository.SaveAsync(gp.Id, gp)
                            .AwaitOrWrap(_ => new CreatingQuickGameFailedException(matchmaking)))
            );

            return gameAggregate;
        }

        var error = game.ErrorValue;

        throw error switch
        {
            _ => new CreatingQuickGameFailedException(matchmaking)
        };
    }
}