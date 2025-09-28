using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.GameCompetitions;
using App.Domain.Game;
using Microsoft.FSharp.Collections;

namespace App.Application.Policy.GameCompetitionStartlist;

public class Classic(IGames games, IGameCompetitionResultsArchive gameCompetitionResultsArchive)
    : IGameCompetitionStartlist
{
    public async Task<IReadOnlyList<JumperId>> Get(Guid gameId, GameCompetitionDto competition, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(gameId), ct).AwaitOrWrap(_ => new IdNotFoundException(gameId));

        switch (competition)
        {
            case PreDraftDto(var competitionIndex):
                var isFirstCompetition = competitionIndex == 0;
                if (isFirstCompetition)
                {
                    return ExtractJumpersIdsFromGame(game);
                }
                
                var preDraftResults = await gameCompetitionResultsArchive.GetPreDraftResultsAsync(gameId, ct);
                if (preDraftResults is null)
                {
                    throw new Exception($"Game {gameId} does not have pre-draft results in archive.");
                }

                var previousCompetitionIndex = competitionIndex - 1;
                var preDraftCompetitionResultRecords =
                    ExtractPreDraftCompetitionResults(preDraftResults, previousCompetitionIndex);
                return GetStartlistByResultRecordsList(preDraftCompetitionResultRecords);

            case MainCompetitionDto:
                preDraftResults = await gameCompetitionResultsArchive.GetPreDraftResultsAsync(gameId, ct);
                if (preDraftResults is null)
                {
                    throw new Exception($"Game {gameId} does not have pre-draft results in archive.");
                }

                var lastPreDraftCompetitionResults = ExtractLastPreDraftCompetitionResults(preDraftResults);
                return GetStartlistByResultRecordsList(lastPreDraftCompetitionResults);
            default:
                throw new Exception($"Unknown competition type {competition}");
        }
    }

    private static List<ArchiveJumperResult> ExtractLastPreDraftCompetitionResults(List<ArchiveCompetitionResultsDto> preDraftResults)
    {
        return ExtractPreDraftCompetitionResults(preDraftResults, preDraftResults.Count - 1);
    }

    private static List<ArchiveJumperResult> ExtractPreDraftCompetitionResults(List<ArchiveCompetitionResultsDto> preDraftResults,
        int competitionIndex)
    {
        var preDraftCompetitionResults = preDraftResults[competitionIndex];
        return preDraftCompetitionResults.JumperResults;
    }

    private static FSharpList<JumperId> ExtractJumpersIdsFromGame(Domain.Game.Game game)
    {
        return JumpersModule.toIdsList(game.Jumpers);
    }

    private static IReadOnlyList<JumperId> GetStartlistByResultRecordsList(List<ArchiveJumperResult> archiveResultRecords)
    {
        var descendingResultRecords =
            archiveResultRecords.OrderByDescending(resultRecord => resultRecord.Rank);
        var sortedStartlist = descendingResultRecords
            .Select(ResultRecordToGameJumperId).ToList().AsReadOnly();
        return sortedStartlist;
    }

    private static JumperId ResultRecordToGameJumperId(ArchiveJumperResult archiveJumperResult)
    {
        return JumperId.NewJumperId(archiveJumperResult.GameJumperId);
    }
}