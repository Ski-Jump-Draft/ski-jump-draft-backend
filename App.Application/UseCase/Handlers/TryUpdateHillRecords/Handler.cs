using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Repositories;
using App.Domain.Time;

namespace App.Application.UseCase.Handlers.TryUpdateHillRecords;

public record Command(Domain.GameWorld.HillTypes.Id HillId, Domain.GameWorld.HillTypes.Record PotentialRecord)
    : ICommand;

public class Handler(IClock clock, IGameWorldHillRepository gameWorldHills) : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        var potentialRecord = command.PotentialRecord;
        var hill = await gameWorldHills.GetByIdAsync(command.HillId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.HillId.Item));

        var day = Domain.GameWorld.HillTypes.RecordModule.Day.NewDay(clock.UtcNow);

        var (updatedHill, events) =
            hill.TryUpdateInGameRecords(day, potentialRecord.Distance, potentialRecord.Setter);

        await gameWorldHills.SaveAsync(updatedHill.Id_, updatedHill, events, messageContext.CorrelationId,
            messageContext.CausationId, ct);
    }
}