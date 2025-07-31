using App.Domain.Draft;

namespace App.Application.Commanding;

public record MapResult(bool Found, Domain.Game.Id.Id? GameId);

public interface IDraftToGameMapStore
{
    Task<MapResult> TryGetGameIdByDraftIdAsync(Id.Id draftId, CancellationToken ct);
    Task AddMappingAsync(Id.Id draftId, Domain.Game.Id.Id gameId, CancellationToken ct);
}