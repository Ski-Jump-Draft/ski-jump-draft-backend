namespace App.Application.Game.GameCompetitions;

public record CompetitionResultsDto(List<ArchiveJumperResult> JumperResults);

public record ArchiveJumperResult(
    Guid GameWorldJumperId,
    Guid GameJumperId,
    Guid CompetitionJumperId,
    int Rank,
    int Bib,
    double Points,
    List<ArchiveJumpResult> Jumps);

public record ArchiveJumpResult(
    Guid Id,
    Guid CompetitionJumperId,
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