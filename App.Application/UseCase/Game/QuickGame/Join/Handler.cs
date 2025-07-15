using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Projection;
using App.Application.UseCase.Game.Exception;
using App.Domain.Game;
using App.Domain.Shared;
using App.Domain.Repositories;
using App.Domain.Repository;
using GameErrors = App.Domain.Game.GameModule.Error; // taki fajny myk

namespace App.Application.UseCase.Game.QuickGame.Join;

// TODO: Walidacja i nie raw string!
public record Command(
    Guid GameId,
    string Nick
) : ICommand<Participant.Id>;

public class Handler(
    IGameRepository games,
    IGameParticipantRepository gameParticipants,
    Func<string, Participant.Participant> translateNickToGameParticipant,
    IGuid guid
) : IApplicationHandler<Participant.Id, Command>
{
    public async Task<Participant.Id> HandleAsync(Command command, CancellationToken ct)
    {
        var gameId = Id.Id.NewId(command.GameId);
        var game = await FSharpAsyncExt.AwaitOrThrow(games.LoadAsync(gameId, ct), new IdNotFoundException(gameId.Item),
            ct);

        var participant = translateNickToGameParticipant(command.Nick);

        var joinResult = game.Join(participant.Id);

        if (joinResult.IsOk)
        {
            var gameAndEvents = joinResult.ResultValue;

            var correlationId = guid.NewGuid();
            var causationId = correlationId;
            var expectedVersion = game.Version_;

            await FSharpAsyncExt.AwaitOrThrow(
                games.SaveAsync(gameAndEvents.Item1, gameAndEvents.Item2, expectedVersion, correlationId, causationId,
                    ct),
                new JoiningQuickGameFailedException(command.Nick, Reason.Unknown),
                ct
            );
            await FSharpAsyncExt.AwaitOrThrow(gameParticipants.SaveAsync(participant),
                new JoiningQuickGameFailedException(command.Nick, Reason.ErrorDuringPreservingParticipant), ct);
            return participant.Id;
        }

        var error = joinResult.ErrorValue;

        throw error switch
        {
            GameErrors.ParticipantAlreadyJoined => new GameFullException(game),
            GameErrors.EndingMatchmakingTooFewParticipants =>
                new ParticipantAlreadyJoinedException(game, participant.Id),
            GameErrors.InvalidPhase invalidPhaseError => new JoiningGameInvalidPhaseException(
                invalidPhaseError.Expected.ToList(), invalidPhaseError.Actual),
            _ => new JoiningQuickGameFailedException(command.Nick, Reason.NoServerAvailable)
        };
    }
}