using App;

namespace App.Application.Abstractions;

public record MapResult(bool Found, Domain.Game.Id.Id? GameId);

public interface IPreDraftToGameMapStore
{
    Task<MapResult> TryGetGameIdAsync(Domain.PreDraft.Id.Id draftId, CancellationToken ct);
    Task AddMappingAsync(Domain.PreDraft.Id.Id draftId, Domain.Game.Id.Id gameId, CancellationToken ct);
}

public interface IDraftToGameMapStore
{
    Task<MapResult> TryGetGameIdAsync(Domain.Draft.Id.Id draftId, CancellationToken ct);
    Task AddMappingAsync(Domain.Draft.Id.Id draftId, Domain.Game.Id.Id gameId, CancellationToken ct);
}

public interface ICompetitionToGameMapStore
{
    Task<MapResult> TryGetGameIdAsync(Domain.SimpleCompetition.CompetitionId draftId, CancellationToken ct);
    Task AddMappingAsync(Domain.SimpleCompetition.CompetitionId draftId, Domain.Game.Id.Id gameId, CancellationToken ct);
}