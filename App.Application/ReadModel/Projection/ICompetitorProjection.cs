namespace App.Application.ReadModel.Projection;

public interface ICompetitorProjection
{
    Task<CompetitorDto?> GetByCompetitionJumpIdAsync(Domain.SimpleCompetition.JumpModule.Id jumpId);
}

public record CompetitorDto(Guid Id, Guid GameWorldJumperId);