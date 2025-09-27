using App.Application.Game.GameCompetitions;

namespace App.Application.Service;

public record PreDraftPositions(
    Dictionary<Guid, List<int>> PositionsByGameJumper
);

public class PreDraftPositionsService(
    IGameCompetitionResultsArchive gameCompetitionResultsArchive)
{
    public async Task<PreDraftPositions> GetPreDraftPositions(Guid gameId, CancellationToken ct = default)
    {
        var positionsByGameJumper = new Dictionary<Guid, List<int>>();
        var preDraftResults = (await gameCompetitionResultsArchive.GetPreDraftResultsAsync(gameId, ct))
                              ?? throw new InvalidOperationException(
                                  $"Game {gameId} does not have pre-draft results in archive.");

        foreach (var preDraftCompetitionResults in preDraftResults)
        {
            foreach (var (_, gameJumperGuid, _, gameJumperPosition, _, _, _) in preDraftCompetitionResults.JumperResults)
            {
                if (!positionsByGameJumper.TryGetValue(gameJumperGuid, out var positionsList))
                    positionsByGameJumper.Add(gameJumperGuid, [gameJumperPosition]);
                else
                    positionsList.Add(gameJumperPosition);
            }
        }

        return new PreDraftPositions(positionsByGameJumper);
    }
}