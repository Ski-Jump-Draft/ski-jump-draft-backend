using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Game;
using App.Domain.Repositories;

namespace App.Application.UseCase.Handlers.EndPreDraft;

public record Command(Id.Id GameId) : ICommand;

public class Handler(IGameRepository games) : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));
        var newGameResult = game.EndPreDraft();
        if (newGameResult.IsOk)
        {
            var (gameAggregate, events) = newGameResult.ResultValue;

            var expectedVersion = game.Version_;

            await games.SaveAsync(gameAggregate.Id_, events, expectedVersion, messageContext.CorrelationId,
                messageContext.CausationId, ct);
        }
    }
}