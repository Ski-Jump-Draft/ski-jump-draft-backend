namespace App.Application.Factory;

public interface ICompetitorsFactory
{
    IEnumerable<Domain.SimpleCompetition.Competitor> Create(Domain.SimpleCompetition.CompetitionId competitionId);
}