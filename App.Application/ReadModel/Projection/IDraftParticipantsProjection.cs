namespace App.Application.ReadModel.Projection;

public interface IDraftParticipantsProjection
{
    Task<DraftParticipantDto?> GetParticipantByIdAsync(Guid participantId, CancellationToken ct);
    Task<IEnumerable<DraftParticipantDto>> GetParticipantsByDraftIdAsync(Guid draftId, CancellationToken ct);
}

public record DraftParticipantDto(Guid DraftParticipantId);