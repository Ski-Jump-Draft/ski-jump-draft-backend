namespace App.Application.ReadModel.Projection;

public interface ICompetitionGameWorldHillProjection
{
    Task<CompetitionGameWorldHillDto?> GetByCompetitionId(Domain.SimpleCompetition.CompetitionId competitionId);
}

public record CompetitionGameWorldHillDto(
    Guid Id
);