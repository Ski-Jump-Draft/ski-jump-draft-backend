using App.Application.Acl;
using App.Application.Game.GameCompetitions;

namespace App.Application.Service;

public record PreDraftPositions(
    Dictionary<Guid, List<int>> PositionsByGameJumper
);

public class PreDraftPositionsService(
    IGameCompetitionResultsArchive gameCompetitionResultsArchive,
    ICompetitionJumperAcl competitionJumperAcl,
    IGameJumperAcl gameJumperAcl)
{
    public PreDraftPositions GetPreDraftPositions(Guid gameId)
    {
        var positionsByGameJumper = new Dictionary<Guid, List<int>>();
        var preDraftResults = gameCompetitionResultsArchive.GetPreDraftResults(gameId)
                              ?? throw new InvalidOperationException(
                                  $"Game {gameId} does not have pre-draft results in archive.");

        foreach (var preDraftCompetitionResults in preDraftResults)
        {
            foreach (var (competitionJumperId, gameJumperPosition, _) in preDraftCompetitionResults.Results)
            {
                var gameJumperId = competitionJumperAcl.GetGameJumper(competitionJumperId).Id;
                if (!positionsByGameJumper.TryGetValue(gameJumperId, out var positionsList))
                    positionsByGameJumper.Add(gameJumperId, [gameJumperPosition]);
                else
                    positionsList.Add(gameJumperPosition);
            }
        }

        return new PreDraftPositions(positionsByGameJumper);
    }
}