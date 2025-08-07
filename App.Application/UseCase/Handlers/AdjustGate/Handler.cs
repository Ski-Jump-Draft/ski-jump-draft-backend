using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Helper;
using App.Domain.Game;
using App.Domain.Repositories;

namespace App.Application.UseCase.Handlers.AdjustGate;

public record Command(Id.Id GameId) : ICommand;

public class Handler(
    ICompetitionRepository competitions,
    IGameCompetitionProjection gameCompetitionProjection,
    ICompetitionGateAdjuster competitionGateAdjuster) : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        var gameCompetitionDto = await gameCompetitionProjection.GetActiveCompetitionByGameIdAsync(command.GameId)
            .AwaitOrWrapNullable(_ => new IdNotFoundException<Guid>(command.GameId.Item));
        var gameCompetitionId = gameCompetitionDto.CompetitionId;
        var gateChange = competitionGateAdjuster.AdjustGate(gameCompetitionId);

        var gameCompetition = await competitions.LoadAsync(gameCompetitionId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(gameCompetitionId.Item));
        var gateChangeResult = gameCompetition.ChangeGateByJury(gateChange);
        if (gateChangeResult.IsOk)
        {
            var (competitionAfterGateChange, events) = gateChangeResult.ResultValue;
            var expectedVersion = competitionAfterGateChange.Version_;

            await competitions.SaveAsync(competitionAfterGateChange.Id_, events, expectedVersion,
                messageContext.CorrelationId,
                messageContext.CausationId, ct);
        }
    }
}