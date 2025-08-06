using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Factory;
using App.Application.UseCase.Exception;
using App.Application.UseCase.Helper;
using App.Domain.Shared;
using App.Domain.Repositories;
using Microsoft.FSharp.Collections;

namespace App.Application.UseCase.Handlers.StartCompetition;

public record Command(
    Domain.Game.Id.Id GameId
) : ICommand<App.Domain.SimpleCompetition.CompetitionId>;

public class Handler(
    IGameRepository games,
    ICompetitionRepository competitions,
    IQuickGameCompetitionSettingsProvider competitionSettingsProvider,
    Domain.SimpleCompetition.IStartingGateSetter startingGateSetter,
    ICompetitorsFactory competitorsFactory,
    ICompetitionTeamsFactory competitionTeamsFactory,
    IGuid guid
) : ICommandHandler<Command, App.Domain.SimpleCompetition.CompetitionId>
{
    public async Task<App.Domain.SimpleCompetition.CompetitionId> HandleAsync(Command command,
        MessageContext messageContext,
        CancellationToken ct)
    {
        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));

        var (competitionType, competitionSettings, hill) = await competitionSettingsProvider.Provide();

        var competitionId = Domain.SimpleCompetition.CompetitionId.NewCompetitionId(guid.NewGuid());

        var startingGate = startingGateSetter.SetStartingGate();

        (Domain.SimpleCompetition.Competition, IEnumerable<Domain.SimpleCompetition.Event.CompetitionEventPayload>)
            competitionAndEvents;
        if (competitionType.IsIndividual)
        {
            var competitors = competitorsFactory.Create(competitionId);
            var competitionResult =
                Domain.SimpleCompetition.Competition.CreateIndividual(competitionId, AggregateVersion.zero,
                    competitionSettings, hill,
                    ListModule.OfSeq(competitors), startingGate);
            if (!competitionResult.IsOk)
                throw new StartingCompetitionFailedException("Error during Individual Competition creation",
                    command.GameId);
            var competitionAndEventsFSharp = competitionResult.ResultValue;
            competitionAndEvents = (competitionAndEventsFSharp.Item1,
                competitionAndEventsFSharp.Item2);
        }
        else
        {
            var teams = competitionTeamsFactory.Create(competitionId);
            var competitionResult =
                Domain.SimpleCompetition.Competition.CreateTeam(competitionId, AggregateVersion.zero,
                    competitionSettings, hill, ListModule.OfSeq(teams),
                    startingGate);
            if (!competitionResult.IsOk)
                throw new StartingCompetitionFailedException("Error during Team Competition creation",
                    command.GameId);
            var competitionAndEventsFSharp = competitionResult.ResultValue;
            competitionAndEvents = (competitionAndEventsFSharp.Item1,
                competitionAndEventsFSharp.Item2);
        }

        var (competition, competitionEvents) = competitionAndEvents;
        var expectedVersion = competition.Version_;
        await competitions.SaveAsync(competition.Id_, ListModule.OfSeq(competitionEvents), expectedVersion,
            messageContext.CorrelationId,
            messageContext.CausationId, ct);

        var gameCompetition = Domain.Game.Competition.Create(competitionId);
        var gameStartCompetitionResult = game.StartCompetition(gameCompetition);
        if (!gameStartCompetitionResult.IsOk)
            throw new StartingCompetitionFailedException("Error during starting the Competition in Game aggregate",
                game.Id_);

        var (gameAggregate, events) = gameStartCompetitionResult.ResultValue;
        expectedVersion = game.Version_;
        await games.SaveAsync(gameAggregate.Id_, events, expectedVersion, messageContext.CorrelationId,
            messageContext.CausationId, ct);
        return competitionId;
    }
}