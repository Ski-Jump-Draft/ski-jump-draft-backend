using App.Application.Game.GameCompetitions;

namespace App.Infrastructure.Archive.GameCompetitionResults;

public class InMemory : IGameCompetitionResultsArchive
{
    private readonly Dictionary<Guid, List<CompetitionResultsDto>> _preDraft = new();
    private readonly Dictionary<Guid, CompetitionResultsDto> _main = new();

    public Task ArchivePreDraftAsync(
        Guid gameId,
        CompetitionResultsDto competitionResults,
        CancellationToken ct = default)
    {
        if (!_preDraft.TryGetValue(gameId, out var competitionResultsDtos))
        {
            competitionResultsDtos = new List<CompetitionResultsDto>();
            _preDraft.Add(gameId, competitionResultsDtos);
        }

        competitionResultsDtos.Add(competitionResults);
        return Task.CompletedTask;
    }

    public Task<List<CompetitionResultsDto>?> GetPreDraftResultsAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        return Task.FromResult(_preDraft.GetValueOrDefault(gameId));
    }

    public Task ArchiveMainAsync(
        Guid gameId,
        CompetitionResultsDto competitionResults,
        CancellationToken ct = default)
    {
        _main[gameId] = competitionResults;
        return Task.CompletedTask;
    }

    public Task<CompetitionResultsDto?> GetMainResultsAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        return Task.FromResult(_main.GetValueOrDefault(gameId));
    }
}