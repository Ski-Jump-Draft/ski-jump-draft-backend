using App.Application.Game.GameCompetitions;

namespace App.Infrastructure.Archive.GameCompetitionResults;

public class InMemory : IGameCompetitionResultsArchive
{
    private readonly Dictionary<Guid, List<CompetitionResultsDto>> _preDraft = new();
    private readonly Dictionary<Guid, CompetitionResultsDto> _main = new();

    public void ArchivePreDraft(Guid gameId, CompetitionResultsDto competitionResults)
    {
        if (!_preDraft.TryGetValue(gameId, out var competitionResultsDtos))
        {
            competitionResultsDtos = new List<CompetitionResultsDto>();
            _preDraft.Add(gameId, competitionResultsDtos);
        }

        competitionResultsDtos.Add(competitionResults);
    }

    public List<CompetitionResultsDto>? GetPreDraftResults(Guid gameId)
    {
        return _preDraft.GetValueOrDefault(gameId);
    }

    public void ArchiveMain(Guid gameId, CompetitionResultsDto competitionResults)
    {
        _main[gameId] = competitionResults;
    }

    public CompetitionResultsDto? GetMainResults(Guid gameId)
    {
        return _main.GetValueOrDefault(gameId);
    }
}