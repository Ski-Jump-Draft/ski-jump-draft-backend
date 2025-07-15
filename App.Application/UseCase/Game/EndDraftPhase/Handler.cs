using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Game;
using App.Domain.Repository;
using App.Domain.Shared;

namespace App.Application.UseCase.Game.EndDraftPhase;

public record Command(Id.Id GameId) : ICommand;

public class Handler(IGameRepository games, IGuid guid) : IApplicationHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var game = await FSharpAsyncExt.AwaitOrThrow(games.LoadAsync(command.GameId, ct),
            new IdNotFoundException(command.GameId.Item), ct);
        var newGameResult = game.EndDraft();
        if (newGameResult.IsOk)
        {
            var (state, events) = newGameResult.ResultValue;

            var correlationId = guid.NewGuid();
            var causationId = correlationId;
            var expectedVersion = game.Version_;

            games.SaveAsync(state, events, expectedVersion, correlationId, causationId, ct);
        }
    }
}