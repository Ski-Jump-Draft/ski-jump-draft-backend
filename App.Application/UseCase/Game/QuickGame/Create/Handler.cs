using App.Application.Abstractions;
using App.Application.Ext;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Game.Exception;
using App.Application.UseCase.Helper;
using App.Domain.Game;
using App.Domain.Shared;
using App.Domain.Repositories;
using Microsoft.FSharp.Collections;

namespace App.Application.UseCase.Game.QuickGame.Create;

public record Command(
    Guid MatchmakingId
) : ICommand<App.Domain.Game.Id.Id>;

public class Handler(
    IGameRepository games,
    IGameParticipantsFactory gameParticipantsFactory,
    IMatchmakingParticipantsProjection matchmakingParticipantsProjection,
    IQuickGameSettingsProvider quickGameSettingsProvider,
    IGuid guid
) : ICommandHandler<Command, App.Domain.Game.Id.Id>
{
    public async Task<App.Domain.Game.Id.Id> HandleAsync(Command command, MessageContext messageContext,
        CancellationToken ct)
    {
        var matchmakingId = Domain.Matchmaking.Id.NewId(command.MatchmakingId);

        var matchmakingParticipantDtos =
            (await matchmakingParticipantsProjection.GetParticipantsByMatchmakingIdAsync(matchmakingId)).ToArray();

        var gameParticipants = gameParticipantsFactory.CreateFromDto(matchmakingParticipantDtos).ToArray();

        var newGameId = Id.Id.NewId(guid.NewGuid());
        var settings = await quickGameSettingsProvider.Provide();

        var gameResult = Domain.Game.Game.Create(newGameId, AggregateVersion.zero, ListModule.OfSeq(gameParticipants),
            settings);

        if (gameResult.IsOk)
        {
            var (game, events) = gameResult.ResultValue;
            var expectedVersion = game.Version_;
            await games.SaveAsync(game.Id_, events, expectedVersion, messageContext.CorrelationId,
                    messageContext.CausationId, ct)
                .AwaitOrWrap(_ => new CreatingQuickGameFailedException(matchmakingId));
            return game.Id_;
        }

        var error = gameResult.ErrorValue;

        throw error switch
        {
            _ => new CreatingQuickGameFailedException(matchmakingId)
        };
    }
}