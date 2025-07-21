using App.Application.Abstractions;
using App.Application.UseCase.Competition.Exception;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Competition;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;
using Microsoft.FSharp.Control;

namespace App.Application.UseCase.Competition.EndCompetition;

public record Command(Domain.Game.Id.Id GameId, Domain.Competition.Jump.Jump Jump) : ICommand;

public class Handler(
    Domain.Competition.Engine.IEngine competitionEngine,
    IGameRepository games,
    IGameCompetitionRepository gameCompetitions,
    IPreDraftCompetitionRepository preDraftCompetitions,
    ICompetitionRepository competitions,
    ICompetitionResultsRepository competitionResultsRepository,
    ICompetitionStartlistRepository competitionStartlists,
    IPreDraftRepository preDrafts,
    ICompetitionEngineSnapshotRepository competitionEnginesSnapshot) : ICommandHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var game = await games.LoadAsync(command.GameId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(command.GameId.Item));
        var competitionId =
            await TryExtractCompetitionIdFromGamePhase(game.Phase_, gameCompetitions, preDraftCompetitions, preDrafts,
                ct);
        var competition = await competitions.LoadAsync(competitionId, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(competitionId.Item));

        var engineSnapshot = competitionEngine.RegisterJump(command.Jump);

        // if (competitionEngine.Actual.IsWaitingForNextRound)
        // {
        //     var shouldEndCompetition = competitionEngine.ShouldEndCompetition;
        //     //competition.EndRound(shouldEndCompetition);
        // }
        // else if (competitionEngine.Actual.IsRunning)
        // {
        var results = ResultsModule.Results.FromState(competition.ResultsId, competitionEngine.ResultsState)
            .ResultValue; // TODO: Niebezpieczny fragment
        await competitionResultsRepository.SaveAsync(results.Id, results);

        var existingStartlist = await competitionStartlists.GetByIdAsync(competition.StartlistId)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(competition.StartlistId.Item));
        var newStartlist = existingStartlist.RemoveFirst();
        await competitionStartlists.SaveAsync(newStartlist.Id_, newStartlist);

        // TODO kiedyś tam:   CompetitionOrder.addForRound(roundIndex, jump/participant)

        // }
        await
            competitionEnginesSnapshot.SaveAsync(competitionEngine.Id, engineSnapshot)
                .AwaitOrWrap(_ => new EngineSnapshotSavingFailedException());
    }

    private static async Task<Domain.Competition.Id.Id> TryExtractCompetitionIdFromGamePhase(
        Domain.Game.GameModule.Phase phase,
        IGameCompetitionRepository gameCompetitions,
        IPreDraftCompetitionRepository preDraftCompetitions,
        IPreDraftRepository preDrafts,
        CancellationToken ct)
    {
        return phase switch
        {
            Domain.Game.GameModule.Phase.Competition gameCompetitionId => await GetCompetitionIdFromGameCompetition(
                gameCompetitions, gameCompetitionId.Item, ct),
            Domain.Game.GameModule.Phase.PreDraft preDraftId => await GetCompetitionIdFromPreDraftPhase(preDrafts,
                preDraftCompetitions, preDraftId.Item, ct),
            _ => throw new InvalidOperationException("Expected is not in competition/pre-draft phase")
        };
    }

    private static async Task<Domain.Competition.Id.Id> GetCompetitionIdFromGameCompetition(
        IGameCompetitionRepository repo,
        Domain.Game.CompetitionModule.Id id,
        CancellationToken ct)
    {
        var gameCompetition = await repo.GetByIdAsync(id)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(id.Item));
        return gameCompetition.CompetitionId;
    }

    private static async Task<Domain.Competition.Id.Id> GetCompetitionIdFromPreDraftPhase(
        IPreDraftRepository preDrafts,
        IPreDraftCompetitionRepository preDraftCompetitions,
        Domain.PreDraft.Id.Id id,
        CancellationToken ct)
    {
        var preDraft = await preDrafts.LoadAsync(id, ct)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(id.Item));

        if (!preDraft.Phase.IsCompetition)
            throw new InvalidOperationException("PreDraft is not in Competition phase");

        var compPhase = (Domain.PreDraft.Phase.Phase.Competition)preDraft.Phase;
        var preDraftCompetition = await preDraftCompetitions.GetByIdAsync(compPhase.CompetitionId)
            .AwaitOrWrap(_ => new IdNotFoundException<Guid>(compPhase.CompetitionId.Item));

        return preDraftCompetition.CompetitionId;
    }
}