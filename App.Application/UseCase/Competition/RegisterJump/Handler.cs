using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Competition;
using App.Domain.Repositories;

namespace App.Application.UseCase.Competition.RegisterJump;

public record Command(Domain.Game.Id.Id GameId, Domain.Competition.Jump.Jump Jump) : ICommand;

public class Handler(
    IGameRepository games,
    ICompetitionRepository competitions) : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, MessageContext messageContext, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));

        if (!game.Phase_.IsCompetition)
        {
            // TODO: Lepszy błąd
            throw new InvalidOperationException("Game is not in Competition phase");
        }

        var gameCompetition = ((Domain.Game.GameModule.Phase.Competition)game.Phase_).Item;
        var competitionId = gameCompetition.CompetitionId_;
        var competition = await competitions.LoadAsync(competitionId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(competitionId.Item));

        var jumpRegistrationResult = competition.RegisterJump(command.Jump);
        if (jumpRegistrationResult.IsOk)
        {
            var (competitionAggreggate, competitionEvents) = jumpRegistrationResult.ResultValue;
            var expectedAggregtateVersion = competition.Version_;
            await competitions.SaveAsync(competitionId, competitionEvents, expectedAggregtateVersion,
                messageContext.CorrelationId,
                messageContext.CausationId, ct);
        }

        // TODO: Kwestia Competition engine snapshots
        //
        // await
        //     competitionEnginesSnapshot.SaveAsync(competitionEngine.Id, engineSnapshot)
        //         .AwaitOrWrap(_ => new EngineSnapshotSavingFailedException());
    }
}