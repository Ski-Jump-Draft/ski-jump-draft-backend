namespace App.Application.Game.GameCompetitions;

public record CompetitionResultsDto(List<ResultRecord> Results);

public record ResultRecord(Guid CompetitionJumperId, int Rank, int Bib, double Points, List<ResultJumpRecord> Jumps);

public record ResultJumpRecord(
    double Distance,
    double Points,
    IReadOnlyList<double>? Judges,
    double? JudgePoints,
    double? WindCompensation,
    double WindAverage,
    double? GateCompensation,
    double? TotalCompensation
);

public interface IGameCompetitionResultsArchive
{
    void ArchivePreDraft(Guid gameId, CompetitionResultsDto competitionResults);
    List<CompetitionResultsDto>? GetPreDraftResults(Guid gameId);
    void ArchiveMain(Guid gameId, CompetitionResultsDto competitionResults);
    CompetitionResultsDto? GetMainResults(Guid gameId);
}