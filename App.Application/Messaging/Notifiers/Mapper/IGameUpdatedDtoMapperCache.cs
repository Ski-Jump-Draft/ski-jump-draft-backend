namespace App.Application.Messaging.Notifiers.Mapper;

public interface IGameUpdatedDtoMapperCache
{
    Task<EndedPreDraftDto?> GetEndedPreDraft(Guid gameId, CancellationToken ct = default);
    Task SetEndedPreDraft(Guid gameId, EndedPreDraftDto preDraftDto, CancellationToken ct = default);
}