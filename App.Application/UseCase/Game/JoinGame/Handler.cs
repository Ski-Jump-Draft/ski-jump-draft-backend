using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.UseCase.Game.Exception;
using App.Domain.Game;
using App.Domain.Shared;
using App.Domain.Repositories;
using App.Domain.Repository;
using GameErrors = App.Domain.Game.GameModule.Error; // taki fajny myk

namespace App.Application.UseCase.Game.JoinGame;

public record Command(
    App.Domain.Profile.User.Id UserId,
    App.Domain.Game.Id.Id GameId
);

public class Handler(
    IGameRepository games,
    IUserRepository users,
    IUserTranslator<Participant.Participant> translateUserToGameParticipant,
    IGuid guid
)
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var userId = command.UserId;
        var newGameParticipant = await translateUserToGameParticipant.CreateTranslatedAsync(userId);

        var gameId = command.GameId;

        var game = await FSharpAsyncExt.AwaitOrThrow(games.LoadAsync(gameId, ct), new IdNotFoundException(gameId.Item),
            ct);

        var joinResult = game.Join(newGameParticipant.Id);

        if (joinResult.IsOk)
        {
            var gameAndEvents = joinResult.ResultValue;

            var correlationId = guid.NewGuid();
            var causationId = correlationId;
            var expectedVersion = game.Version;

            await FSharpAsyncExt.AwaitOrThrow(
                games.SaveAsync(gameAndEvents.Item1, gameAndEvents.Item2, expectedVersion, correlationId, causationId,
                    ct),
                new JoiningGameFailedUnknownException(userId, gameAndEvents.Item1),
                ct
            );
        }
        else
        {
            var error = joinResult.ErrorValue;

            throw error switch
            {
                GameErrors.PlayerAlreadyJoined => new GameFullException(game),
                GameErrors.EndingMatchmakingTooFewPlayers => new PlayerAlreadyJoinedException(game, userId),
                GameErrors.InvalidPhase invalidPhaseError => new JoiningGameInvalidPhaseException(
                    invalidPhaseError.Expected.ToList(), invalidPhaseError.Actual),
                _ => new JoiningGameFailedUnknownException(userId, game)
            };
        }
    }
}