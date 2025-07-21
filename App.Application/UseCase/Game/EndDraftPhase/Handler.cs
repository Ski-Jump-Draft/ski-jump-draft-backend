using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Game;
using App.Domain.Repositories;
using App.Domain.Shared;

namespace App.Application.UseCase.Game.EndDraftPhase;

public record Command(Id.Id GameId) : ICommand;

public class Handler(IGameRepository games, IGuid guid) : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));
        var newGameResult = game.EndDraft();
        if (newGameResult.IsOk)
        {
            var (state, events) = newGameResult.ResultValue;

            var correlationId = guid.NewGuid();
            var causationId = correlationId;
            var expectedVersion = game.Version_;

            await games.SaveAsync(state, events, expectedVersion, correlationId, causationId, ct);
        }
    }
}