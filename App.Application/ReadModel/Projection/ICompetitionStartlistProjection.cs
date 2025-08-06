namespace App.Application.ReadModel.Projection;

public interface ICompetitionStartlistProjection
{
    Task<NextCompetitorDto?> GetNextCompetitorByCompetitionIdAsync(Domain.SimpleCompetition.CompetitionId competitionId);
}

public record NextCompetitorDto(Guid Id);