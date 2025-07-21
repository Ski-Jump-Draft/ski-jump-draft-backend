using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Projection;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Game.Exception;
using App.Domain.Game;
using App.Domain.Game.Hosting;
using App.Domain.Shared;
using App.Domain.Repositories;
using Microsoft.FSharp.Collections;
using Settings = App.Domain.Game.Settings;

namespace App.Application.UseCase.Game.CreateQuickGame;

public record Command(
    Guid MatchmakingId
) : ICommand<App.Domain.Game.Game>;

public class Handler(
    IGameRepository games,
    IGameParticipantRepository gameParticipantsRepository,
    IMatchmakingRepository matchmakings,
    IMatchmakingParticipantsProjection matchmakingParticipantsProjection,
    Func<MatchmakingParticipantDto, App.Domain.Game.Participant.Participant>
        translateMatchmakingParticipantDtoToGameParticipant,
    Func<Guid> getGlobalHostId,
    Func<Settings.Settings> getGameSettings,
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
            matchmakingParticipantDtos.Select(translateMatchmakingParticipantDtoToGameParticipant).ToArray();
        var gameParticipantIds = gameParticipants.Select(participant => participant.Id);

        var gameId = Id.Id.NewId(guid.NewGuid());
        var gameVersion = AggregateVersion.AggregateVersion.NewAggregateVersion(0u);
        var hostId = HostModule.Id.NewId(getGlobalHostId());
        var settings = getGameSettings();

        var game = Domain.Game.Game.Create(gameId, gameVersion, ListModule.OfSeq(gameParticipantIds), hostId, settings);

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
                        gameParticipantsRepository.SaveAsync(gp.Id, gp)
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