namespace App.Application.UseCase.Helper;

public interface ICompetitionGateAdjuster
{
    Domain.SimpleCompetition.GateChange AdjustGate(Domain.SimpleCompetition.CompetitionId competitionId);
}