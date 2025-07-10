using App.Application.Competition.Exception;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Competition;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;
using Microsoft.FSharp.Control;

namespace App.Application.Competition.RegisterJump;

public record Command(Domain.Game.GameModule.Id GameId, Domain.Competition.Jump.Jump Jump);

public class Handler(
    Domain.Competition.Engine.IEngine competitionEngine,
    IGameRepository games,
    IGameCompetitionRepository gameCompetitions,
    IPreDraftCompetitionRepository preDraftCompetitions,
    ICompetitionRepository competitions,
    ICompetitionResultsRepository competitionResultsRepository,
    ICompetitionStartlistRepository competitionStartlists,
    IPreDraftRepository preDrafts,
    ICompetitionEngineSnapshotRepository competitionEnginesSnapshot,
    IClock clock,
    IGuid guid) : IApplicationHandler<Command>
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var game = await FSharpAsyncExt.AwaitOrThrow(games.GetByIdAsync(command.GameId),
            new IdNotFoundException(command.GameId.Item), ct);
        var competitionId =
            await TryExtractCompetitionIdFromGamePhase(game.Phase, gameCompetitions, preDraftCompetitions, preDrafts,
                ct);
        var competition = await FSharpAsyncExt.AwaitOrThrow(competitions.GetById(competitionId),
            new IdNotFoundException(competitionId.Item), ct);

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
        await FSharpAsync.StartAsTask(competitionResultsRepository.Update(results), null, null);

        var existingStartlist = await FSharpAsyncExt.AwaitOrThrow(
            competitionStartlists.GetById(competition.StartlistId),
            new IdNotFoundException(competition.StartlistId.Item), ct);
        var newStartlist = existingStartlist.RemoveFirst();
        await FSharpAsync.StartAsTask(competitionStartlists.Update(newStartlist), null, null);

        // TODO kiedyś tam:   CompetitionOrder.addForRound(roundIndex, jump/participant)

        // }
        await FSharpAsyncExt.AwaitOrThrow(competitionEnginesSnapshot.SaveSnapshotById(competitionEngine.Id, engineSnapshot),
            new EngineSnapshotSavingFailedException(), ct);
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
        var gameCompetition = await FSharpAsyncExt.AwaitOrThrow(repo.GetById(id),
            new IdNotFoundException(id.Item), ct);
        return gameCompetition.CompetitionId;
    }

    private static async Task<Domain.Competition.Id.Id> GetCompetitionIdFromPreDraftPhase(
        IPreDraftRepository preDrafts,
        IPreDraftCompetitionRepository preDraftCompetitions,
        Domain.PreDraft.PreDraftModule.Id id,
        CancellationToken ct)
    {
        var preDraft = await FSharpAsyncExt.AwaitOrThrow(preDrafts.GetById(id),
            new IdNotFoundException(id.Item), ct);

        if (!preDraft.Phase.IsCompetition)
            throw new InvalidOperationException("PreDraft is not in Competition phase");

        var compPhase = (Domain.PreDraft.PreDraftModule.Phase.Competition)preDraft.Phase;
        var preDraftCompetition = await FSharpAsyncExt.AwaitOrThrow(
            preDraftCompetitions.GetById(compPhase.CompetitionId),
            new IdNotFoundException(compPhase.CompetitionId.Item), ct);

        return preDraftCompetition.CompetitionId;
    }
}