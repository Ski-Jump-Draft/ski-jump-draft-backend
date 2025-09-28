namespace App.Application.Game.GameCompetitions;

public record ArchiveCompetitionResultsDto(List<ArchiveJumperResult> JumperResults);

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
    int Gate,
    double? GateCompensation,
    double? TotalCompensation
);

public interface IGameCompetitionResultsArchive
{
    Task ArchivePreDraftAsync(Guid gameId, ArchiveCompetitionResultsDto archiveCompetitionResults, CancellationToken ct);
    Task<List<ArchiveCompetitionResultsDto>?> GetPreDraftResultsAsync(Guid gameId, CancellationToken ct);
    Task ArchiveMainAsync(Guid gameId, ArchiveCompetitionResultsDto archiveCompetitionResults, CancellationToken ct);
    Task<ArchiveCompetitionResultsDto?> GetMainResultsAsync(Guid gameId, CancellationToken ct);
}