using App.Application.Game.GameCompetitions;

namespace App.Infrastructure.Archive.GameCompetitionResults;

public class InMemory : IGameCompetitionResultsArchive
{
    private readonly Dictionary<Guid, List<ArchiveCompetitionResultsDto>> _preDraft = new();
    private readonly Dictionary<Guid, ArchiveCompetitionResultsDto> _main = new();

    public Task ArchivePreDraftAsync(
        Guid gameId,
        ArchiveCompetitionResultsDto archiveCompetitionResults,
        CancellationToken ct = default)
    {
        if (!_preDraft.TryGetValue(gameId, out var competitionResultsDtos))
        {
            competitionResultsDtos = new List<ArchiveCompetitionResultsDto>();
            _preDraft.Add(gameId, competitionResultsDtos);
        }

        competitionResultsDtos.Add(archiveCompetitionResults);
        return Task.CompletedTask;
    }

    public Task<List<ArchiveCompetitionResultsDto>?> GetPreDraftResultsAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        return Task.FromResult(_preDraft.GetValueOrDefault(gameId));
    }

    public Task ArchiveMainAsync(
        Guid gameId,
        ArchiveCompetitionResultsDto archiveCompetitionResults,
        CancellationToken ct = default)
    {
        _main[gameId] = archiveCompetitionResults;
        return Task.CompletedTask;
    }

    public Task<ArchiveCompetitionResultsDto?> GetMainResultsAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        return Task.FromResult(_main.GetValueOrDefault(gameId));
    }
}