using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Factory;
using App.Application.ReadModel.Projection;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Simulating;
using App.Util;

namespace App.Application.UseCase.Game.SimulateJump;

public record Command(Domain.Game.Id.Id GameId) : ICommand;

public class Handler(
    ISimulator simulator,
    ICompetitionJumpFactory competitionJumpFactory,
    IGameCompetitionProjection gameCompetitionProjection,
    ICompetitionRepository competitions,
    IGuid guid) : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var gameCompetitionDto = await gameCompetitionProjection.GetActiveCompetitionByGameIdAsync(command.GameId);
        if (gameCompetitionDto is null)
        {
            throw new InvalidOperationException($"Game (id {command.GameId.Item}) does not have active Competitions");
        }

        var gameCompetitionId = Domain.SimpleCompetition.CompetitionId.NewCompetitionId(gameCompetitionDto.GameId);
        var gameCompetition = await competitions.LoadAsync(gameCompetitionId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(gameCompetitionId.Item));

        var nextCompetitorOption = gameCompetition.NextCompetitor;
        if (nextCompetitorOption.IsNone())
        {
            throw new InvalidOperationException(
                "There is no competitor to jump. It should not throw. Please report this bug.");
        }

        var nextCompetitor = nextCompetitorOption.Value;

        var simulatedJump = simulator.Simulate(Context.NewContext([]));
        var competitionJump = competitionJumpFactory.Create(simulatedJump);

        var jumpResultId = Domain.SimpleCompetition.JumpResultModule.Id.NewId(guid.NewGuid());

        var newGameCompetitionResult = gameCompetition.AddJump(jumpResultId, nextCompetitor.Id_, competitionJump);
        if (newGameCompetitionResult.IsOk)
        {
            var (competitionAggreggate, competitionEvents) = newGameCompetitionResult.ResultValue;

            var expectedAggregtateVersion = competitionAggreggate.Version_;
            await competitions.SaveAsync(competitionAggreggate.Id_, competitionEvents, expectedAggregtateVersion,
                messageContext.CorrelationId,
                messageContext.CausationId, ct);
        }
    }
}