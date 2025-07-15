using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.UseCase.Game.Exception;
using App.Domain.Game;
using App.Domain.Shared;
using App.Domain.Repositories;
using App.Domain.Repository;
using GameErrors = App.Domain.Game.GameModule.Error; // taki fajny myk

namespace App.Application.UseCase.Game.Leave;

public record Command(
    Id.Id GameId,
    Participant.Id ParticipantId
) : ICommand;

public class Handler(
    IGameRepository games,
    IGameParticipantRepository gameParticipants,
    IGuid guid
) : IApplicationHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var gameId = command.GameId;
        var game = await FSharpAsyncExt.AwaitOrThrow(games.LoadAsync(gameId, ct), new IdNotFoundException(gameId.Item),
            ct);

        var participantId = command.ParticipantId;
        var participant = await FSharpAsyncExt.AwaitOrThrow(gameParticipants.GetByIdAsync(participantId),
            new IdNotFoundException(participantId.Item), ct);

        var leaveResult = game.Leave(command.ParticipantId);

        if (leaveResult.IsOk)
        {
            var gameAndEvents = leaveResult.ResultValue;

            var correlationId = guid.NewGuid();
            var causationId = correlationId;
            var expectedVersion = game.Version_;

            await FSharpAsyncExt.AwaitOrThrow(
                games.SaveAsync(gameAndEvents.Item1, gameAndEvents.Item2, expectedVersion, correlationId, causationId,
                    ct),
                new LeavingGameFailedException(gameId, participantId),
                ct
            );

            await FSharpAsyncExt.AwaitOrThrow(gameParticipants.RemoveAsync(participantId),
                new LeavingGameFailedException(gameId, participantId), ct);
        }

        var error = leaveResult.ErrorValue;

        throw error switch
        {
            GameErrors.ParticipantAlreadyJoined => new GameFullException(game),
            GameErrors.EndingMatchmakingTooFewParticipants => new ParticipantAlreadyJoinedException(game,
                participant.Id),
            GameErrors.InvalidPhase invalidPhaseError => new JoiningGameInvalidPhaseException(
                invalidPhaseError.Expected.ToList(), invalidPhaseError.Actual),
            GameErrors.ParticipantNotInGame => new ParticipantNotInGameException(game, participant.Id),
            _ => new LeavingGameFailedException(gameId, participantId)
        };
    }
}