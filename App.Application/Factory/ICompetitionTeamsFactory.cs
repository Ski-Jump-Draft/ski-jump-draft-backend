namespace App.Application.Factory;

public interface ICompetitionTeamsFactory
{
    IEnumerable<Domain.SimpleCompetition.Team> Create(Domain.SimpleCompetition.CompetitionId competitionId);
}