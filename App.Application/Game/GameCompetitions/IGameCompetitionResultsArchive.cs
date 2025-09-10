namespace App.Application.Game.GameCompetitions;

public record CompetitionResultsDto(List<ResultRecord> Results);

public record ResultRecord(Guid CompetitionJumperId, int Position, double Points);

public interface IGameCompetitionResultsArchive
{
    void ArchivePreDraft(Guid gameId, CompetitionResultsDto competitionResults);
    List<CompetitionResultsDto>? GetPreDraftResults(Guid gameId);
    void ArchiveMain(Guid gameId, CompetitionResultsDto competitionResults);
    CompetitionResultsDto? GetMainResults(Guid gameId);
}