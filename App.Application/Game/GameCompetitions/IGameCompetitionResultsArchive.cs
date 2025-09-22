namespace App.Application.Game.GameCompetitions;

public record CompetitionResultsDto(List<ResultRecord> Results);

public record ResultRecord(
    Guid GameWorldJumperId,
    Guid GameJumperId,
    Guid CompetitionJumperId,
    int Rank,
    int Bib,
    double Points,
    List<ResultJumpRecord> Jumps);

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
    Task ArchivePreDraftAsync(Guid gameId, CompetitionResultsDto competitionResults, CancellationToken ct);
    Task<List<CompetitionResultsDto>?> GetPreDraftResultsAsync(Guid gameId, CancellationToken ct);
    Task ArchiveMainAsync(Guid gameId, CompetitionResultsDto competitionResults, CancellationToken ct);
    Task<CompetitionResultsDto?> GetMainResultsAsync(Guid gameId, CancellationToken ct);
}