using App.Application.Game.GameCompetitions;
using App.Domain.Competition;

namespace App.Application.Mapping;

public static class CompetitionClassificationMappers
{
    public static CompetitionResultsDto ToGameCompetitionResultsArchiveDto(
        this IEnumerable<Classification.JumperClassificationResult> jumperClassificationResults)
    {
        return new CompetitionResultsDto(jumperClassificationResults.Select(jumperClassificationResult =>
            new ResultRecord(jumperClassificationResult.JumperId.Item,
                Classification.PositionModule.value(jumperClassificationResult.Position),
                jumperClassificationResult.Points.Item)).ToList());
    }
}