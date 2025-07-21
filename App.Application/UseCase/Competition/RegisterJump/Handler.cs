using App.Application.Abstractions;
using App.Application.UseCase.Competition.Exception;
using App.Application.Exception;
using App.Application.Ext;
using App.Domain.Competition;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Time;
using Microsoft.FSharp.Control;

namespace App.Application.UseCase.Competition.RegisterJump;

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

        var game = await FSharpAsyncExt.AwaitOrThrow(games.LoadAsync(command.GameId, ct),
            new IdNotFoundException<Guid>(command.GameId.Item), ct);
        var competitionId =
            await TryExtractCompetitionIdFromGamePhase(game.Phase_, gameCompetitions, preDraftCompetitions, preDrafts,
                ct);
        var competition = await FSharpAsyncExt.AwaitOrThrow(competitions.LoadAsync(competitionId, ct),
            new IdNotFoundException<Guid>(competitionId.Item), ct);

        var engineSnapshot = competitionEngine.RegisterJump(command.Jump);

        var results = ResultsModule.Results.FromState(competition.ResultsId, competitionEngine.ResultsState)
            .ResultValue; // TODO: Niebezpieczny fragment
        await FSharpAsync.StartAsTask(competitionResultsRepository.UpdateAsync(results), null, null);

        var existingStartlist = await FSharpAsyncExt.AwaitOrThrow(
            competitionStartlists.GetByIdAsync(competition.StartlistId),
            new IdNotFoundException<Guid>(competition.StartlistId.Item), ct);
        var newStartlist = existingStartlist.RemoveFirst();
        await FSharpAsync.StartAsTask(competitionStartlists.UpdateAsync(newStartlist), null, null);

        // TODO kiedyś tam:   CompetitionOrder.addForRound(roundIndex, jump/participant)

        // }
        await FSharpAsyncExt.AwaitOrThrow(
            competitionEnginesSnapshot.SaveByIdAsync(competitionEngine.Id, engineSnapshot),
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
        var gameCompetition = await FSharpAsyncExt.AwaitOrThrow(repo.GetByIdAsync(id),
            new IdNotFoundException<Guid>(id.Item), ct);
        return gameCompetition.CompetitionId;
    }

    private static async Task<Domain.Competition.Id.Id> GetCompetitionIdFromPreDraftPhase(
        IPreDraftRepository preDrafts,
        IPreDraftCompetitionRepository preDraftCompetitions,
        Domain.PreDraft.Id.Id id,
        CancellationToken ct)
    {
        var preDraft = await FSharpAsyncExt.AwaitOrThrow(preDrafts.LoadAsync(id, ct),
            new IdNotFoundException<Guid>(id.Item), ct);

        if (!preDraft.Phase.IsCompetition)
            throw new InvalidOperationException("PreDraft is not in Competition phase");

        var compPhase = (Domain.PreDraft.Phase.Phase.Competition)preDraft.Phase;
        var preDraftCompetition = await FSharpAsyncExt.AwaitOrThrow(
            preDraftCompetitions.GetByIdAsync(compPhase.CompetitionId),
            new IdNotFoundException<Guid>(compPhase.CompetitionId.Item), ct);

        return preDraftCompetition.CompetitionId;
    }
}